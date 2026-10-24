using Aluguel.Domain.Fiscal;

namespace Aluguel.Application.Common.Validacoes;

/// <summary>Validação dos códigos fiscais NFS-e (cTribNac, cIndOp, cClassTrib).</summary>
public static class NfseCodigoFiscal
{
    public static string? NormalizarDigitos(string? valor, int tamanhoExato)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var digitos = Documento.SomenteDigitos(valor);
        if (string.IsNullOrEmpty(digitos) || digitos.Length != tamanhoExato) return null;
        return digitos;
    }

    public static bool EhCodigoTributacaoNacional(string? valor) =>
        NormalizarDigitos(valor, 6) is not null;

    public static bool EhCodigoIndicadorOperacao(string? valor) =>
        NormalizarDigitos(valor, 6) is not null;

    public static bool EhClassificacaoTributaria(string? valor) =>
        NormalizarDigitos(valor, 6) is not null;

    public static string MensagemCTribNac =>
        $"Código de tributação nacional (cTribNac) deve ter 6 dígitos (padrão recomendado: {NfseCodigosPadrao.CodigoTributacaoNacional}).";

    public static string MensagemCIndOp =>
        $"Código indicador da operação (cIndOp) deve ter 6 dígitos (padrão recomendado: {NfseCodigosPadrao.CodigoIndicadorOperacao}).";

    public static string MensagemCClassTrib =>
        $"Classificação tributária (cClassTrib) deve ter 6 dígitos (padrão recomendado: {NfseCodigosPadrao.ClassificacaoTributaria}).";
}
