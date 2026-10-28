namespace Aluguel.Application.Abstractions;

/// <summary>
/// Porta da API Nacional da NFS-e (SEFIN): monta/assina DPS e emite de forma síncrona.
/// </summary>
public interface INfseNacionalProvider
{
    /// <summary>Emite NFS-e a partir dos dados da cobrança/prestador (mTLS A1 + DPS assinada).</summary>
    Task<NfseEmissaoResultado> EmitirAsync(NfseEmissaoPedido pedido, CancellationToken ct = default);

    /// <summary>
    /// Cancela NFS-e via evento e101101 na SEFIN (<c>POST /nfse/{chave}/eventos</c>).
    /// </summary>
    Task<NfseCancelamentoResultado> CancelarAsync(NfseCancelamentoPedido pedido, CancellationToken ct = default);
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
    string SenhaCertificado,
    NfseDadosImovel? Imovel = null);

/// <summary>Dados do imóvel para IBSCBS/imovel (obrigatório com cIndOp de locação, ex. 020201).</summary>
public sealed record NfseDadosImovel(
    string CodigoMunicipioIbge,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? InscricaoImobiliaria,
    /// <summary>cNBS de 9 dígitos (residencial 110021000 / não residencial 110022000).</summary>
    string? CodigoNbs = null,
    decimal PercentualCopropriedade = 100m);

public sealed record NfsePartePrestador(
    string Documento,
    bool PessoaJuridica,
    string? InscricaoMunicipal,
    string? RazaoSocial,
    string? Telefone,
    string? Email,
    string? CodigoServico,
    string? CodigoIndicadorOperacao,
    string? ClassificacaoTributaria,
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

/// <summary>Pedido de cancelamento (evento e101101) assinado com o A1 do emitente.</summary>
public sealed record NfseCancelamentoPedido(
    string ChaveAcesso,
    string DocumentoAutor,
    bool AutorPessoaJuridica,
    string Motivo,
    /// <summary>cMotivo: 1=Erro na emissão, 2=Serviço não prestado, 9=Outros.</summary>
    string CodigoMotivo,
    byte[] CertificadoPfx,
    string SenhaCertificado);

/// <summary>Resultado síncrono do evento de cancelamento na SEFIN (ou mock).</summary>
public sealed record NfseCancelamentoResultado(
    bool Sucesso,
    string? Protocolo,
    byte[]? XmlEvento,
    string? MensagemErro);
