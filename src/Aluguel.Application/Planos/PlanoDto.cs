namespace Aluguel.Application.Planos;

public sealed record PlanoDto(
    Guid Id,
    string Codigo,
    string Nome,
    int? MaxImoveis,
    int? MaxUsuarios,
    bool PermiteNfse,
    int TrialDias,
    decimal? ValorMensal);
