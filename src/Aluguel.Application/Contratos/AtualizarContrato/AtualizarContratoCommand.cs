using Aluguel.Application.Abstractions;
using Aluguel.Application.Auditoria;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Auditoria;
using MediatR;

namespace Aluguel.Application.Contratos.AtualizarContrato;

/// <summary>
/// Atualiza cláusulas do contrato (somente enquanto Ativo). Imóvel, inquilino e número são imutáveis.
/// </summary>
public sealed record AtualizarContratoCommand(
    Guid Id,
    DateOnly? DataFimPrevista,
    int DiaVencimento,
    decimal ValorAluguel,
    decimal? JurosAtrasoPct,
    decimal? MultaAtrasoPct,
    string? AnexoPath) : IRequest<ContratoDto?>;

public sealed class AtualizarContratoCommandHandler(
    IContratoRepository repositorio,
    ICurrentUser currentUser,
    IAuditoriaService auditoria)
    : IRequestHandler<AtualizarContratoCommand, ContratoDto?>
{
    public async Task<ContratoDto?> Handle(AtualizarContratoCommand request, CancellationToken cancellationToken)
    {
        var contrato = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (contrato is null || !EscopoCliente.PodeAcessar(currentUser, contrato.ClienteId))
            return null;

        var antes = AuditoriaSnapshots.DeContrato(contrato.ParaDto());

        contrato.Atualizar(
            request.DataFimPrevista,
            request.DiaVencimento,
            request.ValorAluguel,
            request.JurosAtrasoPct,
            request.MultaAtrasoPct,
            request.AnexoPath);

        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        var dto = contrato.ParaDto();
        await auditoria.RegistrarAsync(
            AcaoAuditoria.Alteracao, TabelaAuditoria.Contrato, dto.Id, dto.ClienteId,
            antes, AuditoriaSnapshots.DeContrato(dto),
            resumo: $"Contrato «{dto.NumeroContrato}» alterado",
            cancellationToken: cancellationToken);
        return dto;
    }
}
