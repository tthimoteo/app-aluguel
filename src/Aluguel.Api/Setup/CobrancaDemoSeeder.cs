using System.Text;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Cobrancas.BaixarArquivoCobranca;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using Aluguel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Api.Setup;

/// <summary>Em Development, gera cobranças de demonstração (NFS-e + docs + boleto) se a tabela estiver vazia.</summary>
public static class CobrancaDemoSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        if (await db.NotasFiscais.IgnoreQueryFilters().AnyAsync())
            return;

        var cliente = await db.Clientes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Nome == "Cliente Demo" || c.Id == Guid.Parse("e6c2a056-b096-4b51-852c-278a460bea4b"));
        if (cliente is null) return;

        var imovel = await db.Imoveis.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ClienteId == cliente.Id);
        if (imovel is null) return;

        var inquilino = await db.Inquilinos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ClienteId == cliente.Id);

        var gestor = await db.Users.FirstOrDefaultAsync(u => u.Email == "gestor@demo.local");
        var emissorId = gestor?.Id ?? Guid.Empty;
        var agora = DateTimeOffset.UtcNow;

        await CriarCobrancaAsync(db, storage, cliente.TenantId, cliente.Id, imovel.Id, inquilino?.Id,
            "09/2026", 2500m, 9001, emissorId, agora.AddMonths(-1), comPagamento: true);
        await CriarCobrancaAsync(db, storage, cliente.TenantId, cliente.Id, imovel.Id, inquilino?.Id,
            "10/2026", 2500m, 9002, emissorId, agora, comPagamento: false);

        await db.SaveChangesAsync();
    }

    private static async Task CriarCobrancaAsync(
        AppDbContext db, IFileStorage storage,
        Guid tenantId, Guid clienteId, Guid imovelId, Guid? inquilinoId,
        string competencia, decimal valor, long numero, Guid emissorId,
        DateTimeOffset emitidaEm, bool comPagamento)
    {
        var nfse = new NotaFiscalServico(tenantId, clienteId, imovelId, competencia, valor,
            inquilinoId: inquilinoId);
        nfse.MarcarEmitida(numero, "1", $"NFe{numero}DEMO{competencia.Replace("/", "")}", emissorId, emitidaEm);
        db.NotasFiscais.Add(nfse);

        var xmlBytes = Encoding.UTF8.GetBytes(
            $"""<?xml version="1.0" encoding="UTF-8"?><NFSe><Numero>{numero}</Numero><Competencia>{competencia}</Competencia><Valor>{valor:F2}</Valor></NFSe>""");
        var pdfBytes = Encoding.UTF8.GetBytes($"%PDF-1.4\n% NFS-e {numero} competencia {competencia}\n");
        var boletoBytes = Encoding.UTF8.GetBytes($"%PDF-1.4\n% Boleto competencia {competencia} valor {valor:F2}\n");

        var xmlRel = $"{tenantId:D}/{nfse.Id:D}.xml";
        var pdfRel = $"{tenantId:D}/{nfse.Id:D}.pdf";
        var boletoRel = $"{tenantId:D}/{nfse.Id:D}-boleto.pdf";

        string xmlPath, pdfPath, boletoPath;
        await using (var s = new MemoryStream(xmlBytes))
            xmlPath = await storage.UploadAsync(BaixarArquivoCobrancaQueryHandler.BucketNfseXml, xmlRel, s, "application/xml");
        await using (var s = new MemoryStream(pdfBytes))
            pdfPath = await storage.UploadAsync(BaixarArquivoCobrancaQueryHandler.BucketNfsePdf, pdfRel, s, "application/pdf");
        await using (var s = new MemoryStream(boletoBytes))
            boletoPath = await storage.UploadAsync(BaixarArquivoCobrancaQueryHandler.BucketBoletos, boletoRel, s, "application/pdf");

        db.DocumentosFiscais.Add(new DocumentoFiscal(tenantId, nfse.Id, TipoDocumentoFiscal.XML, xmlPath,
            tamanhoBytes: xmlBytes.Length));
        db.DocumentosFiscais.Add(new DocumentoFiscal(tenantId, nfse.Id, TipoDocumentoFiscal.PDF, pdfPath,
            tamanhoBytes: pdfBytes.Length));

        // Vencimento = dia 10 no mês seguinte à competência (mesmo critério da geração real).
        var venc = Aluguel.Application.Cobrancas.CobrancaVencimento.Calcular(competencia, dia: 10);
        var boleto = new Boleto(tenantId, clienteId, imovelId, competencia, valor, venc, nfse.Id,
            nossoNumero: $"BOL{numero}", linhaDigitavel: $"23790.00000 00000.000000 00000.000000 {numero % 10} {numero}");
        boleto.DefinirPdf(boletoPath);
        db.Boletos.Add(boleto);

        if (comPagamento)
        {
            boleto.MarcarPago();
            db.Pagamentos.Add(new Pagamento(tenantId, clienteId, imovelId, competencia, valor,
                venc.AddDays(2), nfseId: nfse.Id));
        }
    }
}
