using System.Security.Cryptography.X509Certificates;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Fiscal;
using MediatR;

namespace Aluguel.Application.Clientes.EnviarCertificadoCliente;

/// <summary>
/// Upload do certificado digital A1 (PFX/P12) + senha. Arquivo em bucket <c>certificates</c>;
/// senha cifrada só no backend (§5).
/// </summary>
public sealed record EnviarCertificadoClienteCommand(
    Guid ClienteId,
    Stream Conteudo,
    string NomeArquivo,
    string ContentType,
    string Senha) : IRequest<CertificadoDigitalDto?>;

public sealed class EnviarCertificadoClienteCommandHandler(
    IClienteRepository clientes,
    ICertificadoDigitalRepository certificados,
    IFileStorage storage,
    ISecretProtector protector,
    ICurrentUser currentUser) : IRequestHandler<EnviarCertificadoClienteCommand, CertificadoDigitalDto?>
{
    public const string Bucket = "certificates";
    public const long TamanhoMaximoBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<CertificadoDigitalDto?> Handle(
        EnviarCertificadoClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null || !EscopoCliente.PodeAcessar(currentUser, cliente.Id))
            return null;

        if (string.IsNullOrWhiteSpace(request.Senha))
            throw new InvalidOperationException("Informe a senha do certificado A1.");

        ValidarArquivo(request.NomeArquivo, request.ContentType, request.Conteudo);

        await using var buffer = new MemoryStream();
        await request.Conteudo.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0)
            throw new InvalidOperationException("O arquivo do certificado está vazio.");
        if (bytes.Length > TamanhoMaximoBytes)
            throw new InvalidOperationException("O certificado deve ter no máximo 5 MB.");

        X509Certificate2 cert;
        try
        {
            cert = X509CertificateLoader.LoadPkcs12(bytes, request.Senha, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception)
        {
            throw new InvalidOperationException(
                "Não foi possível abrir o certificado. Verifique se o arquivo é PFX/P12 e se a senha está correta.");
        }

        using (cert)
        {
            var thumbprint = cert.Thumbprint ?? throw new InvalidOperationException("Certificado sem thumbprint.");
            var validade = new DateTimeOffset(cert.NotAfter.ToUniversalTime());

            foreach (var ativo in await certificados.ListarAtivosPorClienteAsync(cliente.Id, cancellationToken))
                ativo.Desativar();

            buffer.Position = 0;
            var pathRelativo = $"{cliente.TenantId:D}/{cliente.Id:D}/{Guid.NewGuid():N}.pfx";
            var caminho = await storage.UploadAsync(
                Bucket, pathRelativo, buffer, "application/x-pkcs12", cancellationToken);

            var senhaCifrada = protector.Protect(request.Senha);
            var entidade = new CertificadoDigital(
                cliente.TenantId, cliente.Id, caminho, thumbprint, validade, senhaCifrada);
            certificados.Adicionar(entidade);
            await certificados.SalvarAlteracoesAsync(cancellationToken);
            return entidade.ParaDto();
        }
    }

    private static void ValidarArquivo(string nomeArquivo, string contentType, Stream conteudo)
    {
        if (conteudo.CanSeek && conteudo.Length == 0)
            throw new InvalidOperationException("O arquivo do certificado está vazio.");
        if (conteudo.CanSeek && conteudo.Length > TamanhoMaximoBytes)
            throw new InvalidOperationException("O certificado deve ter no máximo 5 MB.");

        var extensao = Path.GetExtension(nomeArquivo)?.ToLowerInvariant() ?? "";
        var contentOk = string.IsNullOrWhiteSpace(contentType)
            || contentType.Contains("pkcs12", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("x-pkcs12", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/x-pfx", StringComparison.OrdinalIgnoreCase);
        if (extensao is not (".pfx" or ".p12") || !contentOk)
            throw new InvalidOperationException("Envie o certificado A1 em formato PFX ou P12.");
    }
}
