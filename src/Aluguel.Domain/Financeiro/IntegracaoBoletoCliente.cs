using System.Text.Json;
using Aluguel.Domain.Common;

namespace Aluguel.Domain.Financeiro;

/// <summary>
/// Credenciais e configuração da API bancária usada pelo cliente para emitir boletos.
/// Um cliente tem no máximo uma integração ativa; o provedor é escolhido no catálogo expansível.
/// </summary>
public class IntegracaoBoletoCliente : Entity, ITenantOwned, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    /// <summary>Código estável do provedor (ex.: itau, bradesco) — ver catálogo.</summary>
    public string ProvedorCodigo { get; private set; } = default!;
    public bool Ativo { get; private set; } = true;
    /// <summary>Campos públicos (agência, conta, carteira…) em JSON.</summary>
    public string ConfiguracaoPublicaJson { get; private set; } = "{}";
    /// <summary>Campos secretos (client_secret, tokens…) cifrados via <c>ISecretProtector</c>.</summary>
    public byte[] SegredosCifrados { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private IntegracaoBoletoCliente() { }

    public IntegracaoBoletoCliente(
        Guid tenantId,
        Guid clienteId,
        string provedorCodigo,
        string configuracaoPublicaJson,
        byte[] segredosCifrados)
    {
        if (string.IsNullOrWhiteSpace(provedorCodigo))
            throw new ArgumentException("Provedor de boleto é obrigatório.", nameof(provedorCodigo));
        if (segredosCifrados is null || segredosCifrados.Length == 0)
            throw new ArgumentException("Segredos cifrados são obrigatórios.", nameof(segredosCifrados));

        TenantId = tenantId;
        ClienteId = clienteId;
        ProvedorCodigo = NormalizarCodigo(provedorCodigo);
        ConfiguracaoPublicaJson = string.IsNullOrWhiteSpace(configuracaoPublicaJson)
            ? "{}"
            : configuracaoPublicaJson.Trim();
        SegredosCifrados = segredosCifrados;
        Ativo = true;
    }

    public void Atualizar(string provedorCodigo, string configuracaoPublicaJson, byte[] segredosCifrados)
    {
        if (string.IsNullOrWhiteSpace(provedorCodigo))
            throw new ArgumentException("Provedor de boleto é obrigatório.", nameof(provedorCodigo));
        if (segredosCifrados is null || segredosCifrados.Length == 0)
            throw new ArgumentException("Segredos cifrados são obrigatórios.", nameof(segredosCifrados));

        ProvedorCodigo = NormalizarCodigo(provedorCodigo);
        ConfiguracaoPublicaJson = string.IsNullOrWhiteSpace(configuracaoPublicaJson)
            ? "{}"
            : configuracaoPublicaJson.Trim();
        SegredosCifrados = segredosCifrados;
        Ativo = true;
        Touch();
    }

    public void Desativar()
    {
        Ativo = false;
        Touch();
    }

    public IReadOnlyDictionary<string, string> LerConfiguracaoPublica()
    {
        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(ConfiguracaoPublicaJson);
            return map ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    public static string NormalizarCodigo(string codigo) =>
        codigo.Trim().ToLowerInvariant().Replace(' ', '_');
}
