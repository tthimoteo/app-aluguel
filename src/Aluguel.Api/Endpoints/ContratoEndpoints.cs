using Aluguel.Application.Autorizacao;
using Aluguel.Application.Contratos.AnexarDocumentoContrato;
using Aluguel.Application.Contratos.AtualizarContrato;
using Aluguel.Application.Contratos.BaixarAnexoContrato;
using Aluguel.Application.Contratos.CancelarContrato;
using Aluguel.Application.Contratos.CriarContrato;
using Aluguel.Application.Contratos.EncerrarContrato;
using Aluguel.Application.Contratos.ListarContratos;
using Aluguel.Application.Contratos.ObterContratoPorId;
using Aluguel.Domain.Contratos;
using MediatR;

namespace Aluguel.Api.Endpoints;

public sealed record CriarContratoRequest(
    Guid ImovelId,
    Guid InquilinoId,
    string NumeroContrato,
    DateOnly DataInicio,
    DateOnly? DataFimPrevista,
    int DiaVencimento,
    decimal ValorAluguel,
    decimal? JurosAtrasoPct,
    decimal? MultaAtrasoPct);

public sealed record AtualizarContratoRequest(
    DateOnly? DataFimPrevista,
    int DiaVencimento,
    decimal ValorAluguel,
    decimal? JurosAtrasoPct,
    decimal? MultaAtrasoPct,
    string? AnexoPath);

public static class ContratoEndpoints
{
    public static IEndpointRouteBuilder MapContratoEndpoints(this IEndpointRouteBuilder app)
    {
        // Leitura liberada a qualquer perfil autenticado; escrita/transições: Administrador/Gestor.
        var grupo = app.MapGroup("/api/contratos")
            .WithTags("Contratos")
            .RequireAuthorization();

        grupo.MapGet("/", async (Guid? clienteId, Guid? imovelId, Guid? inquilinoId, string? status,
            int? skip, int? take, ISender sender, CancellationToken ct) =>
        {
            StatusContrato? statusFiltro = Enum.TryParse<StatusContrato>(status, ignoreCase: true, out var s) ? s : null;
            var pagina = await sender.Send(
                new ListarContratosQuery(clienteId, imovelId, inquilinoId, statusFiltro, skip ?? 0, take ?? 20), ct);
            return Results.Ok(pagina);
        })
        .WithName("ListarContratos");

        grupo.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var contrato = await sender.Send(new ObterContratoPorIdQuery(id), ct);
            return contrato is null ? Results.NotFound() : Results.Ok(contrato);
        })
        .WithName("ObterContratoPorId");

        grupo.MapPost("/", async (CriarContratoRequest req, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new CriarContratoCommand(
                req.ImovelId, req.InquilinoId, req.NumeroContrato, req.DataInicio, req.DataFimPrevista,
                req.DiaVencimento, req.ValorAluguel, req.JurosAtrasoPct, req.MultaAtrasoPct), ct);
            return Results.Created($"/api/contratos/{dto.Id}", dto);
        })
        .WithName("CriarContrato")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapPut("/{id:guid}", async (Guid id, AtualizarContratoRequest req, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new AtualizarContratoCommand(
                id, req.DataFimPrevista, req.DiaVencimento, req.ValorAluguel,
                req.JurosAtrasoPct, req.MultaAtrasoPct, req.AnexoPath), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("AtualizarContrato")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapPost("/{id:guid}/anexo", async (Guid id, HttpRequest http, ISender sender, CancellationToken ct) =>
        {
            if (!http.HasFormContentType)
                return Results.BadRequest(new { erro = "Envie o arquivo em multipart/form-data (campo 'arquivo')." });

            var form = await http.ReadFormAsync(ct);
            var arquivo = form.Files.GetFile("arquivo") ?? form.Files.FirstOrDefault();
            if (arquivo is null || arquivo.Length == 0)
                return Results.BadRequest(new { erro = "Selecione o PDF do contrato." });

            await using var stream = arquivo.OpenReadStream();
            try
            {
                var dto = await sender.Send(new AnexarDocumentoContratoCommand(
                    id, stream, arquivo.FileName, arquivo.ContentType ?? "application/pdf"), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        })
        .WithName("AnexarDocumentoContrato")
        .DisableAntiforgery()
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapGet("/{id:guid}/anexo", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var arquivo = await sender.Send(new BaixarAnexoContratoQuery(id), ct);
                if (arquivo is null)
                    return Results.NotFound(new { erro = "Anexo do contrato não encontrado." });

                // fileDownloadName força Content-Disposition: attachment (download do PDF antigo ou atual).
                return Results.File(
                    arquivo.Conteudo,
                    contentType: "application/pdf",
                    fileDownloadName: arquivo.NomeArquivo.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                        ? arquivo.NomeArquivo
                        : $"{arquivo.NomeArquivo}.pdf",
                    enableRangeProcessing: false);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound(new { erro = "Arquivo do anexo não está disponível no storage." });
            }
        })
        .WithName("BaixarAnexoContrato");

        grupo.MapPost("/{id:guid}/encerrar", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new EncerrarContratoCommand(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("EncerrarContrato")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapPost("/{id:guid}/cancelar", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new CancelarContratoCommand(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("CancelarContrato")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        return app;
    }
}
