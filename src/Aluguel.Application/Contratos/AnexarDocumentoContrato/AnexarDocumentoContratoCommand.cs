using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Contratos.AnexarDocumentoContrato;

/// <summary>
/// Anexa o PDF do contrato (UC004 / §9). Upload mediado pela API → storage (bucket <c>contracts</c>).
/// Caminho: <c>contracts/{tenant}/{cliente}/{contratoId}.pdf</c>.
/// </summary>
public sealed record AnexarDocumentoContratoCommand(
    Guid ContratoId,
    Stream Conteudo,
    string NomeArquivo,
    string ContentType) : IRequest<ContratoDto?>;

public sealed class AnexarDocumentoContratoCommandHandler(
    IContratoRepository contratos,
    IFileStorage storage,
    ICurrentUser currentUser) : IRequestHandler<AnexarDocumentoContratoCommand, ContratoDto?>
{
    public const string Bucket = "contracts";
    public const long TamanhoMaximoBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<ContratoDto?> Handle(AnexarDocumentoContratoCommand request, CancellationToken cancellationToken)
    {
        var contrato = await contratos.ObterPorIdAsync(request.ContratoId, cancellationToken);
        if (contrato is null || !EscopoCliente.PodeAcessar(currentUser, contrato.ClienteId))
            return null;

        ValidarArquivo(request.NomeArquivo, request.ContentType, request.Conteudo);

        var pathRelativo = $"{contrato.TenantId:D}/{contrato.ClienteId:D}/{contrato.Id:D}.pdf";
        var caminhoLogico = await storage.UploadAsync(
            Bucket, pathRelativo, request.Conteudo, "application/pdf", cancellationToken);

        contrato.DefinirAnexo(caminhoLogico);
        await contratos.SalvarAlteracoesAsync(cancellationToken);
        return contrato.ParaDto();
    }

    private static void ValidarArquivo(string nomeArquivo, string contentType, Stream conteudo)
    {
        if (conteudo.CanSeek && conteudo.Length == 0)
            throw new InvalidOperationException("O arquivo do contrato está vazio.");
        if (conteudo.CanSeek && conteudo.Length > TamanhoMaximoBytes)
            throw new InvalidOperationException("O anexo do contrato deve ter no máximo 10 MB.");

        var extensao = Path.GetExtension(nomeArquivo)?.ToLowerInvariant() ?? "";
        var contentOk = string.IsNullOrWhiteSpace(contentType)
            || contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);
        if (extensao != ".pdf" || !contentOk)
            throw new InvalidOperationException("Anexe o contrato em PDF (.pdf).");
    }
}
