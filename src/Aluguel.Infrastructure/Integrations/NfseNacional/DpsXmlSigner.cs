using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Assinatura XMLDSig (RSA-SHA256) da DPS — Signature como irmã de infDPS.</summary>
public static class DpsXmlSigner
{
    private const string Sha256Digest = "http://www.w3.org/2001/04/xmlenc#sha256";

    public static string Assinar(string xmlDps, string idReferencia, X509Certificate2 certificado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlDps);
        ArgumentException.ThrowIfNullOrWhiteSpace(idReferencia);
        ArgumentNullException.ThrowIfNull(certificado);

        using var rsa = certificado.GetRSAPrivateKey()
            ?? throw new InvalidOperationException(
                "O certificado A1 não expõe chave privada RSA para assinatura XMLDSig.");

        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(xmlDps);

        var signedXml = new IdAwareSignedXml(document) { SigningKey = rsa };
        signedXml.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigExcC14NWithCommentsTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        var reference = new Reference
        {
            Uri = $"#{idReferencia}",
            DigestMethod = Sha256Digest
        };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NWithCommentsTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificado));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();

        if (document.DocumentElement is null)
            throw new InvalidOperationException("XML da DPS sem elemento raiz.");

        var signatureNode = signedXml.GetXml();
        document.DocumentElement.AppendChild(document.ImportNode(signatureNode, deep: true));

        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            OmitXmlDeclaration = false
        }))
        {
            document.Save(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class IdAwareSignedXml(XmlDocument document) : SignedXml(document)
    {
        public override XmlElement? GetIdElement(XmlDocument? doc, string idValue) =>
            base.GetIdElement(doc, idValue) ?? FindById(doc, idValue);

        private static XmlElement? FindById(XmlDocument? doc, string idValue)
        {
            if (doc?.DocumentElement is null) return null;
            foreach (XmlElement el in doc.GetElementsByTagName("*"))
            {
                if (string.Equals(el.GetAttribute("Id"), idValue, StringComparison.Ordinal))
                    return el;
            }

            return null;
        }
    }
}
