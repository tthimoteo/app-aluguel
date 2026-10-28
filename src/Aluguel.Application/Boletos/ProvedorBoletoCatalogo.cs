namespace Aluguel.Application.Boletos;

/// <summary>Tipo de campo exibido no cadastro da integração bancária.</summary>
public enum TipoCampoProvedorBoleto
{
    Texto = 0,
    Segredo = 1,
    Numero = 2,
}

/// <summary>Definição de um campo do formulário de integração (catálogo expansível).</summary>
public sealed record CampoProvedorBoletoDef(
    string Chave,
    string Rotulo,
    TipoCampoProvedorBoleto Tipo,
    bool Obrigatorio,
    string? Placeholder = null,
    string? Ajuda = null);

/// <summary>Provedor de boleto registrado no catálogo (banco/fintech).</summary>
public sealed record ProvedorBoletoDef(
    string Codigo,
    string Nome,
    /// <summary>Código FEBRABAN (3 dígitos) quando aplicável; vazio para fintechs sem banco próprio.</summary>
    string CodigoBanco,
    IReadOnlyList<CampoProvedorBoletoDef> Campos);

/// <summary>
/// Catálogo estático e expansível de provedores de boleto.
/// Novos bancos: adicionar uma entrada aqui + adapter em Infrastructure.
/// </summary>
public static class ProvedorBoletoCatalogo
{
    private static readonly IReadOnlyList<ProvedorBoletoDef> TodosInterno =
    [
        new("itau", "Itaú", "341",
        [
            Campo("client_id", "Client ID", TipoCampoProvedorBoleto.Texto, true, "App Key Itaú"),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("agencia", "Agência", TipoCampoProvedorBoleto.Texto, true, "0000"),
            Campo("conta", "Conta", TipoCampoProvedorBoleto.Texto, true),
            Campo("digito_conta", "Dígito da conta", TipoCampoProvedorBoleto.Texto, true, "0"),
            Campo("carteira", "Carteira", TipoCampoProvedorBoleto.Texto, true, "109"),
            Campo("codigo_beneficiario", "Código do beneficiário", TipoCampoProvedorBoleto.Texto, false),
        ]),
        new("bradesco", "Bradesco", "237",
        [
            Campo("client_id", "Client ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("agencia", "Agência", TipoCampoProvedorBoleto.Texto, true),
            Campo("conta", "Conta", TipoCampoProvedorBoleto.Texto, true),
            Campo("carteira", "Carteira", TipoCampoProvedorBoleto.Texto, true, "09"),
            Campo("codigo_beneficiario", "Código do beneficiário", TipoCampoProvedorBoleto.Texto, false),
        ]),
        new("banco_do_brasil", "Banco do Brasil", "001",
        [
            Campo("client_id", "Client ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("developer_application_key", "Developer Application Key", TipoCampoProvedorBoleto.Segredo, true,
                ajuda: "AppKey do portal developers.bb.com.br"),
            Campo("convenio", "Convênio", TipoCampoProvedorBoleto.Texto, true),
            Campo("carteira", "Carteira", TipoCampoProvedorBoleto.Texto, true, "17"),
            Campo("variacao_carteira", "Variação da carteira", TipoCampoProvedorBoleto.Texto, false, "019"),
        ]),
        new("santander", "Santander", "033",
        [
            Campo("client_id", "Client ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("workspace_id", "Workspace ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("convenio", "Convênio / station", TipoCampoProvedorBoleto.Texto, true),
            Campo("conta", "Conta", TipoCampoProvedorBoleto.Texto, true),
            Campo("digito_conta", "Dígito da conta", TipoCampoProvedorBoleto.Texto, false),
        ]),
        new("caixa", "Caixa Econômica Federal", "104",
        [
            Campo("client_id", "Client ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("convenio", "Convênio", TipoCampoProvedorBoleto.Texto, true),
            Campo("agencia", "Agência", TipoCampoProvedorBoleto.Texto, true),
            Campo("codigo_beneficiario", "Código do beneficiário", TipoCampoProvedorBoleto.Texto, true),
            Campo("carteira", "Carteira / modalidade", TipoCampoProvedorBoleto.Texto, false, "14"),
        ]),
        new("sicredi", "Sicredi", "748",
        [
            Campo("client_id", "Client ID / API Key", TipoCampoProvedorBoleto.Texto, true),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("cooperativa", "Cooperativa", TipoCampoProvedorBoleto.Texto, true, "0001"),
            Campo("posto", "Posto", TipoCampoProvedorBoleto.Texto, true, "00"),
            Campo("codigo_beneficiario", "Código do beneficiário", TipoCampoProvedorBoleto.Texto, true),
            Campo("carteira", "Carteira", TipoCampoProvedorBoleto.Texto, false, "1"),
        ]),
        new("mercado_pago", "Mercado Pago", "",
        [
            Campo("access_token", "Access Token", TipoCampoProvedorBoleto.Segredo, true,
                ajuda: "Token de produção ou teste da aplicação Mercado Pago"),
            Campo("user_id", "User ID (opcional)", TipoCampoProvedorBoleto.Texto, false),
        ]),
        new("inter", "Banco Inter", "077",
        [
            Campo("client_id", "Client ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("conta_corrente", "Conta corrente", TipoCampoProvedorBoleto.Texto, true),
            Campo("scopes", "Scopes OAuth (opcional)", TipoCampoProvedorBoleto.Texto, false,
                "boleto-cobranca.read boleto-cobranca.write"),
        ]),
        new("c6", "C6 Bank", "336",
        [
            Campo("client_id", "Client ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("account_id", "Account ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("agencia", "Agência", TipoCampoProvedorBoleto.Texto, false),
            Campo("conta", "Conta", TipoCampoProvedorBoleto.Texto, false),
        ]),
        new("nubank", "Nubank", "260",
        [
            Campo("client_id", "Client ID", TipoCampoProvedorBoleto.Texto, true),
            Campo("client_secret", "Client Secret", TipoCampoProvedorBoleto.Segredo, true),
            Campo("conta_id", "ID da conta / account", TipoCampoProvedorBoleto.Texto, true),
        ]),
    ];

    public static IReadOnlyList<ProvedorBoletoDef> Todos => TodosInterno;

    public static ProvedorBoletoDef? Obter(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var c = codigo.Trim().ToLowerInvariant().Replace(' ', '_');
        return TodosInterno.FirstOrDefault(p => p.Codigo == c);
    }

    public static bool Existe(string codigo) => Obter(codigo) is not null;

    private static CampoProvedorBoletoDef Campo(
        string chave,
        string rotulo,
        TipoCampoProvedorBoleto tipo,
        bool obrigatorio,
        string? placeholder = null,
        string? ajuda = null) =>
        new(chave, rotulo, tipo, obrigatorio, placeholder, ajuda);
}
