using Aluguel.Domain.Contratos;

namespace Aluguel.Application.Contratos;

internal static class ContratoMappings
{
    public static ContratoDto ParaDto(this Contrato c) => new(
        c.Id,
        c.TenantId,
        c.ClienteId,
        c.ImovelId,
        c.InquilinoId,
        c.NumeroContrato,
        c.Status.ToString(),
        c.DataInicio,
        c.DataFimPrevista,
        c.DiaVencimento,
        c.ValorAluguel,
        c.JurosAtrasoPct,
        c.MultaAtrasoPct,
        c.AnexoPath,
        c.CreatedAt,
        c.UpdatedAt);
}
