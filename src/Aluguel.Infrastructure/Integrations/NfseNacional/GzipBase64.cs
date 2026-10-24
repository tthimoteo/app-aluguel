using System.IO.Compression;
using System.Text;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

internal static class GzipBase64
{
    public static string CompactarParaBase64(string utf8Xml)
    {
        var bytes = Encoding.UTF8.GetBytes(utf8Xml);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            gzip.Write(bytes, 0, bytes.Length);
        return Convert.ToBase64String(output.ToArray());
    }

    public static byte[] DescompactarDeBase64(string base64)
    {
        var compressed = Convert.FromBase64String(base64);
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}
