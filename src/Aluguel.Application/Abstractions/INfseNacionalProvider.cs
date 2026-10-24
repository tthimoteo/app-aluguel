namespace Aluguel.Application.Abstractions;

/// <summary>
/// Porta da API Nacional da NFS-e (SEFIN): monta/assina DPS e emite de forma síncrona.
/// </summary>
public interface INfseNacionalProvider
{
    /// <summary>Emite NFS-e a partir dos dados da cobrança/prestador (mTLS A1 + DPS assinada).</summary>
    Task<NfseEmissaoResultado> EmitirAsync(NfseEmissaoPedido pedido, CancellationToken ct = default);
}

/// <summary>Pedido de emissão (prestador = cliente; tomador = inquilino).</summary>
public sealed record NfseEmissaoPedido(
    Guid NfseId,
    string Competencia,
    decimal ValorServico,
    string DescricaoServico,
    NfsePartePrestador Prestador,
    NfseParteTomador Tomador,
    byte[] CertificadoPfx,
    string SenhaCertificado);

public sealed record NfsePartePrestador(
    string Documento,
    bool PessoaJuridica,
    string? InscricaoMunicipal,
    string? RazaoSocial,
    string? Telefone,
    string? Email,
    string? CodigoServico,
    string? RegimeTributario,
    string CodigoMunicipioIbge,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro);

public sealed record NfseParteTomador(
    string Documento,
    bool PessoaJuridica,
    string Nome,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
    string? CodigoMunicipioIbge,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro);

/// <summary>Resultado síncrono da SEFIN (ou mock local).</summary>
public sealed record NfseEmissaoResultado(
    bool Sucesso,
    long? Numero,
    string? Serie,
    string? ChaveAcesso,
    string? IdDps,
    byte[]? XmlNfse,
    string? XmlDpsEnviado,
    string? MensagemErro);
