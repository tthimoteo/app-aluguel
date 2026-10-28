namespace Aluguel.Application.Abstractions;

/// <summary>Pedido de geração de boleto via API do provedor bancário cadastrado.</summary>
public sealed record BoletoGeracaoPedido(
    Guid ReferenciaId,
    Guid ClienteId,
    string Competencia,
    decimal Valor,
    DateOnly DataVencimento,
    string BeneficiarioNome,
    string BeneficiarioDocumento,
    bool BeneficiarioPj,
    string PagadorNome,
    string PagadorDocumento,
    bool PagadorPj,
    string? Descricao,
    IReadOnlyDictionary<string, string> Credenciais);

/// <summary>Resultado da geração do boleto na API do banco/fintech.</summary>
public sealed record BoletoGeracaoResultado(
    bool Sucesso,
    string? NossoNumero = null,
    string? LinhaDigitavel = null,
    string? CodigoBarras = null,
    byte[]? PdfBytes = null,
    string? IdExterno = null,
    string? MensagemErro = null);

/// <summary>
/// Adapter de um provedor de boleto (Itaú, Bradesco, …).
/// Novos bancos: implementar esta porta e registrar no factory + catálogo.
/// </summary>
public interface IBoletoProvider
{
    string CodigoProvedor { get; }
    Task<BoletoGeracaoResultado> GerarAsync(BoletoGeracaoPedido pedido, CancellationToken ct = default);
}

/// <summary>Resolve o adapter pelo código do provedor cadastrado no cliente.</summary>
public interface IBoletoProviderFactory
{
    IBoletoProvider Obter(string codigoProvedor);
}
