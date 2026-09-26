using System.Security.Cryptography;
using System.Text;
using Aluguel.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Aluguel.Infrastructure.Security;

/// <summary>
/// Cifra segredos com AES-256-GCM. Chave via <c>Security:CertProtectorKey</c>
/// (ou <c>CERT_PROTECTOR_KEY</c>) — 32 bytes em Base64 ou texto (≥32 chars).
/// </summary>
public sealed class AesSecretProtector : ISecretProtector
{
    private readonly byte[] _key;

    public AesSecretProtector(IConfiguration configuration)
    {
        var raw = configuration["Security:CertProtectorKey"]
            ?? configuration["CERT_PROTECTOR_KEY"]
            ?? "aluguel-dev-cert-protector-key-32b!"; // apenas Development
        _key = DerivarChave(raw);
    }

    public byte[] Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);

        var result = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, result, nonce.Length + tag.Length, cipher.Length);
        return result;
    }

    public string Unprotect(byte[] protectedBytes)
    {
        ArgumentNullException.ThrowIfNull(protectedBytes);
        if (protectedBytes.Length < 12 + 16)
            throw new CryptographicException("Payload cifrado inválido.");

        var nonce = protectedBytes.AsSpan(0, 12);
        var tag = protectedBytes.AsSpan(12, 16);
        var cipher = protectedBytes.AsSpan(28);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    private static byte[] DerivarChave(string raw)
    {
        try
        {
            var fromB64 = Convert.FromBase64String(raw);
            if (fromB64.Length == 32) return fromB64;
        }
        catch (FormatException)
        {
            // texto livre
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }
}
