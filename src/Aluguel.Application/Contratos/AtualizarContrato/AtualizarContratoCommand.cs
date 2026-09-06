using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
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

public sealed class AtualizarContratoCommandHandler(IContratoRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<AtualizarContratoCommand, ContratoDto?>
{
    public async Task<ContratoDto?> Handle(AtualizarContratoCommand request, CancellationToken cancellationToken)
    {
        var contrato = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (contrato is null || !EscopoCliente.PodeAcessar(currentUser, contrato.ClienteId))
            return null;

        contrato.Atualizar(
            request.DataFimPrevista,
            request.DiaVencimento,
            request.ValorAluguel,
            request.JurosAtrasoPct,
            request.MultaAtrasoPct,
            string.IsNullOrWhiteSpace(request.AnexoPath) ? null : request.AnexoPath.Trim());

        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return contrato.ParaDto();
    }
}
