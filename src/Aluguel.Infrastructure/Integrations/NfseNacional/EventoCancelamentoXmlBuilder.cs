using System.Globalization;
using System.Xml.Linq;
using Aluguel.Application.Abstractions;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Monta o XML <c>pedRegEvento</c> do cancelamento (e101101) conforme leiaute nacional.</summary>
internal static class EventoCancelamentoXmlBuilder
{
    private static readonly XNamespace Ns = "http://www.sped.fazenda.gov.br/nfse";

    /// <summary>Id = PRE + chave(50) + tpEvento(101) + nPedRegEvento(001).</summary>
    public static string MontarIdPedido(string chaveAcesso, int numeroPedido = 1)
    {
        var chave = SomenteDigitos(chaveAcesso);
        if (chave.Length != 50)
            throw new InvalidOperationException(
                $"Chave de acesso da NFS-e deve ter 50 dígitos (recebido: {chave.Length}).");
        if (numeroPedido is < 1 or > 999)
            throw new ArgumentOutOfRangeException(nameof(numeroPedido));

        return $"PRE{chave}101{numeroPedido:000}";
    }

    public static string Montar(
        NfseCancelamentoPedido pedido,
        NfseOptions options,
        DateTimeOffset emitidoEm,
        out string idPedReg)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        ArgumentNullException.ThrowIfNull(options);

        var chave = SomenteDigitos(pedido.ChaveAcesso);
        idPedReg = MontarIdPedido(chave);
        var docAutor = SomenteDigitos(pedido.DocumentoAutor);
        var motivo = SanitizarMotivo(pedido.Motivo);
        if (motivo.Length < 15)
            motivo = motivo.PadRight(15, '.');
        if (motivo.Length > 255)
            motivo = motivo[..255];

        var cMotivo = string.IsNullOrWhiteSpace(pedido.CodigoMotivo) ? "1" : pedido.CodigoMotivo.Trim();

        XElement autor = pedido.AutorPessoaJuridica
            ? new XElement(Ns + "CNPJAutor", docAutor)
            : new XElement(Ns + "CPFAutor", docAutor);

        var dh = emitidoEm.ToOffset(TimeSpan.FromHours(-3))
            .ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

        var root = new XElement(Ns + "pedRegEvento",
            new XAttribute("versao", "1.00"),
            new XElement(Ns + "infPedReg",
                new XAttribute("Id", idPedReg),
                new XElement(Ns + "tpAmb", options.TipoAmbiente.ToString(CultureInfo.InvariantCulture)),
                new XElement(Ns + "verAplic", Truncar(options.VerAplic, 20)),
                new XElement(Ns + "dhEvento", dh),
                autor,
                new XElement(Ns + "chNFSe", chave),
                new XElement(Ns + "e101101",
                    new XElement(Ns + "xDesc", "Cancelamento de NFS-e"),
                    new XElement(Ns + "cMotivo", cMotivo),
                    new XElement(Ns + "xMotivo", motivo))));

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static string SomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "";
        return new string(valor.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// TSMotivo SEFIN: caracteres Latin-1 de '!' (0x21) a ÿ (0xFF), sem controle.
    /// Substitui travessões tipográficos e demais Unicode fora do intervalo.
    /// </summary>
    private static string SanitizarMotivo(string? raw)
    {
        var s = (raw ?? "").Trim()
            .Replace('\u2014', '-') // —
            .Replace('\u2013', '-') // –
            .Replace('\u2012', '-')
            .Replace('\u00A0', ' ');
        var chars = s.Select(ch => ch is >= '!' and <= 'ÿ' ? ch : ' ').ToArray();
        return new string(chars).Trim();
    }

    private static string Truncar(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max];
}
