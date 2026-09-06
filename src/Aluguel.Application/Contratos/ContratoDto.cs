namespace Aluguel.Application.Contratos;

/// <summary>Representação de leitura de um contrato (resposta da API).</summary>
public sealed record ContratoDto(
    Guid Id,
    Guid TenantId,
    Guid ClienteId,
    Guid ImovelId,
    Guid InquilinoId,
    string NumeroContrato,
    string Status,
    DateOnly DataInicio,
    DateOnly? DataFimPrevista,
    int DiaVencimento,
    decimal ValorAluguel,
    decimal? JurosAtrasoPct,
    decimal? MultaAtrasoPct,
    string? AnexoPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
