namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Resolução simplificada cidade/UF → IBGE (expansível).</summary>
public static class MunicipioIbge
{
    private static readonly Dictionary<string, string> Mapa = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sao paulo|SP"] = "3550308",
        ["são paulo|SP"] = "3550308",
        ["rio de janeiro|RJ"] = "3304557",
        ["belo horizonte|MG"] = "3106200",
        ["brasilia|DF"] = "5300108",
        ["brasília|DF"] = "5300108",
        ["curitiba|PR"] = "4106902",
        ["porto alegre|RS"] = "4314902",
        ["salvador|BA"] = "2927408",
        ["fortaleza|CE"] = "2304400",
        ["recife|PE"] = "2611606",
        ["manaus|AM"] = "1302603",
        ["belem|PA"] = "1501402",
        ["belém|PA"] = "1501402",
        ["goiania|GO"] = "5208707",
        ["goiânia|GO"] = "5208707",
        ["campinas|SP"] = "3509502",
        ["guarulhos|SP"] = "3518800",
        ["osasco|SP"] = "3534401",
        ["santo andre|SP"] = "3547809",
        ["santo andré|SP"] = "3547809",
        ["sao bernardo do campo|SP"] = "3548708",
        ["são bernardo do campo|SP"] = "3548708",
    };

    public static string? Resolver(string? cidade, string? uf, string? fallback = null)
    {
        if (!string.IsNullOrWhiteSpace(cidade) && !string.IsNullOrWhiteSpace(uf))
        {
            var key = $"{cidade.Trim()}|{uf.Trim().ToUpperInvariant()}";
            if (Mapa.TryGetValue(key, out var codigo))
                return codigo;
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
    }
}
