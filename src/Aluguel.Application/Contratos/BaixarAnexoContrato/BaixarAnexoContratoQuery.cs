using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Contratos.BaixarAnexoContrato;

/// <summary>Download mediado do PDF do contrato (bucket contracts). Qualquer perfil autenticado com acesso ao cliente.</summary>
public sealed record BaixarAnexoContratoQuery(Guid ContratoId) : IRequest<AnexoContratoArquivo?>;

public sealed record AnexoContratoArquivo(
    Stream Conteudo,
    string ContentType,
    string NomeArquivo,
    string CaminhoLogico);

public sealed class BaixarAnexoContratoQueryHandler(
    IContratoRepository contratos,
    IFileStorage storage,
    ICurrentUser currentUser) : IRequestHandler<BaixarAnexoContratoQuery, AnexoContratoArquivo?>
{
    public const string Bucket = "contracts";

    public async Task<AnexoContratoArquivo?> Handle(BaixarAnexoContratoQuery request, CancellationToken cancellationToken)
    {
        var contrato = await contratos.ObterPorIdAsync(request.ContratoId, cancellationToken);
        if (contrato is null || !EscopoCliente.PodeAcessar(currentUser, contrato.ClienteId))
            return null;

        if (string.IsNullOrWhiteSpace(contrato.AnexoPath))
            return null;

        var stream = await storage.DownloadAsync(Bucket, contrato.AnexoPath, cancellationToken);
        var nome = Path.GetFileName(contrato.AnexoPath);
        if (string.IsNullOrWhiteSpace(nome))
            nome = $"contrato-{contrato.NumeroContrato}.pdf";

        return new AnexoContratoArquivo(stream, "application/pdf", nome, contrato.AnexoPath);
    }
}
