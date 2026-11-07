using Aluguel.Application.Abstractions;
using Aluguel.Application.Auditoria;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Auditoria;
using Aluguel.Domain.Common;
using MediatR;

namespace Aluguel.Application.Inquilinos.AtualizarInquilino;

/// <summary>Atualiza dados do inquilino. Tipo de pessoa e documento são imutáveis.</summary>
public sealed record AtualizarInquilinoCommand(
    Guid Id,
    string Nome,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
    StatusAtivoInativo Status,
    EnderecoInput? Endereco) : IRequest<InquilinoDto?>;

public sealed class AtualizarInquilinoCommandHandler(
    IInquilinoRepository repositorio,
    ICurrentUser currentUser,
    IAuditoriaService auditoria)
    : IRequestHandler<AtualizarInquilinoCommand, InquilinoDto?>
{
    public async Task<InquilinoDto?> Handle(AtualizarInquilinoCommand request, CancellationToken cancellationToken)
    {
        var inquilino = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (inquilino is null || !EscopoCliente.PodeAcessar(currentUser, inquilino.ClienteId))
            return null;

        var antes = AuditoriaSnapshots.DeInquilino(inquilino.ParaDto());

        inquilino.Atualizar(
            request.Nome.Trim(),
            string.IsNullOrWhiteSpace(request.InscricaoMunicipal) ? null : request.InscricaoMunicipal.Trim(),
            request.Telefone,
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            request.Endereco?.ParaValueObject());

        if (request.Status == StatusAtivoInativo.Ativo)
            inquilino.Ativar();
        else
            inquilino.Inativar();

        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        var dto = inquilino.ParaDto();
        await auditoria.RegistrarAsync(
            AcaoAuditoria.Alteracao, TabelaAuditoria.Inquilino, dto.Id, dto.ClienteId,
            antes, AuditoriaSnapshots.DeInquilino(dto),
            resumo: $"Inquilino «{dto.Nome}» alterado",
            cancellationToken: cancellationToken);
        return dto;
    }
}
