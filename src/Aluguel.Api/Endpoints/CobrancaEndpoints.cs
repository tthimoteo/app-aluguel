using Aluguel.Application.Autorizacao;
using Aluguel.Application.Cobrancas;
using Aluguel.Application.Cobrancas.BaixarArquivoCobranca;
using Aluguel.Application.Cobrancas.GerarCobranca;
using Aluguel.Application.Cobrancas.ListarCobrancas;
using Aluguel.Application.Cobrancas.ObterCobranca;
using MediatR;

namespace Aluguel.Api.Endpoints;

public static class CobrancaEndpoints
{
    public sealed record GerarCobrancaRequest(
        Guid ImovelId,
        string Competencia,
        decimal ValorAluguel,
        bool IncluirCondominio = true,
        decimal? ValorCondominio = null,
        bool IncluirIptu = true,
        decimal? ValorIptu = null,
        int DiaVencimento = 10,
        bool EmitirNfse = true);

    public static IEndpointRouteBuilder MapCobrancaEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/cobrancas")
            .WithTags("Cobranças")
            .RequireAuthorization();

        grupo.MapGet("/", async (Guid? clienteId, Guid? imovelId, string? competencia,
            int? skip, int? take, ISender sender, CancellationToken ct) =>
        {
            var pagina = await sender.Send(new ListarCobrancasQuery(
                clienteId, imovelId, competencia, skip ?? 0, take ?? 20), ct);
            return Results.Ok(pagina);
        })
        .WithName("ListarCobrancas");

        grupo.MapPost("/", async (GerarCobrancaRequest req, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var dto = await sender.Send(new GerarCobrancaCommand(
                    req.ImovelId,
                    req.Competencia,
                    req.ValorAluguel,
                    req.IncluirCondominio,
                    req.ValorCondominio,
                    req.IncluirIptu,
                    req.ValorIptu,
                    req.DiaVencimento,
                    req.EmitirNfse), ct);
                return Results.Created($"/api/cobrancas/{dto.Id}", dto);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        })
        .WithName("GerarCobranca")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new ObterCobrancaQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("ObterCobranca");

        grupo.MapGet("/{id:guid}/nfse.xml", async (Guid id, ISender sender, CancellationToken ct) =>
            await Baixar(id, TipoDownloadCobranca.NfseXml, sender, ct))
        .WithName("BaixarNfseXml");

        grupo.MapGet("/{id:guid}/nfse.pdf", async (Guid id, ISender sender, CancellationToken ct) =>
            await Baixar(id, TipoDownloadCobranca.NfsePdf, sender, ct))
        .WithName("BaixarNfsePdf");

        grupo.MapGet("/{id:guid}/boleto.pdf", async (Guid id, ISender sender, CancellationToken ct) =>
            await Baixar(id, TipoDownloadCobranca.BoletoPdf, sender, ct))
        .WithName("BaixarBoletoPdf");

        return app;
    }

    private static async Task<IResult> Baixar(Guid id, TipoDownloadCobranca tipo, ISender sender, CancellationToken ct)
    {
        try
        {
            var arquivo = await sender.Send(new BaixarArquivoCobrancaQuery(id, tipo), ct);
            if (arquivo is null)
                return Results.NotFound(new { erro = "Arquivo não disponível para esta cobrança." });

            return Results.File(arquivo.Conteudo, arquivo.ContentType, fileDownloadName: arquivo.NomeArquivo);
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound(new { erro = "Arquivo não está disponível no storage." });
        }
    }
}
