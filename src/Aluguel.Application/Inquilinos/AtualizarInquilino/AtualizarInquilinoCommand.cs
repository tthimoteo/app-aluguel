using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
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

public sealed class AtualizarInquilinoCommandHandler(IInquilinoRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<AtualizarInquilinoCommand, InquilinoDto?>
{
    public async Task<InquilinoDto?> Handle(AtualizarInquilinoCommand request, CancellationToken cancellationToken)
    {
        var inquilino = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (inquilino is null || !EscopoCliente.PodeAcessar(currentUser, inquilino.ClienteId))
            return null;

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
        return inquilino.ParaDto();
    }
}
