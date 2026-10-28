namespace Aluguel.Application.Boletos;

public sealed record CampoProvedorBoletoDto(
    string Chave,
    string Rotulo,
    string Tipo,
    bool Obrigatorio,
    string? Placeholder,
    string? Ajuda);

public sealed record ProvedorBoletoDto(
    string Codigo,
    string Nome,
    string CodigoBanco,
    IReadOnlyList<CampoProvedorBoletoDto> Campos);

/// <summary>
/// Integração cadastrada do cliente. Valores de campos <c>Segredo</c> nunca retornam em claro —
/// apenas a flag <see cref="CamposSegredoPreenchidos"/>.
/// </summary>
public sealed record IntegracaoBoletoClienteDto(
    Guid Id,
    Guid ClienteId,
    string ProvedorCodigo,
    string ProvedorNome,
    bool Ativo,
    IReadOnlyDictionary<string, string> CamposPublicos,
    IReadOnlyList<string> CamposSegredoPreenchidos,
    DateTimeOffset UpdatedAt);
