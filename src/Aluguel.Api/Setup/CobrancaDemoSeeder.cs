using System.Text;
using System.Text.Json;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Cobrancas.BaixarArquivoCobranca;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using Aluguel.Domain.Imoveis;
using Aluguel.Domain.Inquilinos;
using Aluguel.Domain.ValueObjects;
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
        var nfsePdf = scope.ServiceProvider.GetRequiredService<INfsePdfGenerator>();
        var protector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        var cliente = await db.Clientes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.Id == Guid.Parse("e6c2a056-b096-4b51-852c-278a460bea4b")
                || c.Cnpj == "15809930000168"
                || c.Nome == "Cliente Demo"
                || c.RazaoSocial == "T THIMOTEO CONSULTORIA E SERVICOS DE ENGENHARIA LTDA");
        if (cliente is null) return;

        // Integração de boleto demo (Bradesco) — permite GerarCobranca mesmo sem cadastro manual.
        if (!await db.IntegracoesBoleto.IgnoreQueryFilters()
                .AnyAsync(i => i.ClienteId == cliente.Id && i.Ativo))
        {
            var publicos = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["agencia"] = "1234",
                ["conta"] = "567890",
                ["carteira"] = "09",
                ["client_id"] = "demo-client-id",
            });
            var segredos = protector.Protect(JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["client_secret"] = "demo-client-secret",
            }));
            db.IntegracoesBoleto.Add(new IntegracaoBoletoCliente(
                cliente.TenantId, cliente.Id, "bradesco", publicos, segredos));
            await db.SaveChangesAsync();
        }

        if (await db.NotasFiscais.IgnoreQueryFilters().AnyAsync())
            return;

        var imovel = await db.Imoveis.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ClienteId == cliente.Id);
        if (imovel is null)
        {
            var end = new Endereco("Rua Faria Lobato", "137", null, "Vila Santa Clara", "São Paulo", "SP", "03159070");
            imovel = new Imovel(cliente.TenantId, cliente.Id, "Apto Demo — Faria Lobato", TipoImovel.Residencial, end);
            imovel.DefinirCobranca("09/2026", PropositoLocacao.Residencial, 10, null, null);
            db.Imoveis.Add(imovel);
            await db.SaveChangesAsync();
        }

        var inquilino = await db.Inquilinos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ClienteId == cliente.Id);
        if (inquilino is null)
        {
            inquilino = new Inquilino(
                cliente.TenantId, cliente.Id, TipoPessoa.PF, "Thiago Thimoteo", "28958973870",
                telefone: "11999990000", email: "tomador@demo.local",
                endereco: new Endereco("Av Paulista", "1000", null, "Bela Vista", "São Paulo", "SP", "01310100"),
                imovelId: imovel.Id);
            db.Inquilinos.Add(inquilino);
            await db.SaveChangesAsync();
        }

        var gestor = await db.Users.FirstOrDefaultAsync(u => u.Email == "gestor@demo.local");
        var emissorId = gestor?.Id ?? Guid.Empty;
        var agora = DateTimeOffset.UtcNow;

        var xmlAutorizado = await CarregarXmlAutorizadoAsync(env);

        await CriarCobrancaAsync(db, storage, nfsePdf, cliente.TenantId, cliente.Id, imovel.Id, inquilino.Id,
            "09/2026", 2500m, 9001, emissorId, agora.AddMonths(-1), comPagamento: true, xmlAutorizado);
        await CriarCobrancaAsync(db, storage, nfsePdf, cliente.TenantId, cliente.Id, imovel.Id, inquilino.Id,
            "10/2026", 2500m, 9002, emissorId, agora, comPagamento: false, xmlAutorizado);

        await db.SaveChangesAsync();
    }

    private static async Task CriarCobrancaAsync(
        AppDbContext db, IFileStorage storage, INfsePdfGenerator nfsePdf,
        Guid tenantId, Guid clienteId, Guid imovelId, Guid? inquilinoId,
        string competencia, decimal valor, long numero, Guid emissorId,
        DateTimeOffset emitidaEm, bool comPagamento, byte[] xmlAutorizado)
    {
        var nfse = new NotaFiscalServico(tenantId, clienteId, imovelId, competencia, valor,
            inquilinoId: inquilinoId);
        // Chave real do XML autorizado SEFIN (fixture) — fallback demo só se o Id do XML não bater.
        var chave = ExtrairChave(xmlAutorizado) ?? $"NFe{numero}DEMO{competencia.Replace("/", "")}";
        nfse.MarcarEmitida(numero, "1", chave, emissorId, emitidaEm);
        db.NotasFiscais.Add(nfse);

        var xmlBytes = xmlAutorizado;
        var pdfBytes = nfsePdf.Gerar(xmlBytes, chave, competencia);
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
        var venc = Aluguel.Domain.Financeiro.CobrancaVencimento.Calcular(competencia, dia: 10);
        var boleto = new Boleto(tenantId, clienteId, imovelId, competencia, valor, venc, nfse.Id,
            nossoNumero: $"BOL{numero}",
            linhaDigitavel: $"23790.00000 00000.000000 00000.000000 {numero % 10} {numero}",
            provedorCodigo: "bradesco",
            idExterno: $"demo-{numero}");
        boleto.DefinirPdf(boletoPath);
        db.Boletos.Add(boleto);

        if (comPagamento)
        {
            boleto.MarcarPago();
            db.Pagamentos.Add(new Pagamento(tenantId, clienteId, imovelId, competencia, valor,
                venc.AddDays(2), nfseId: nfse.Id));
        }
    }

    private static async Task<byte[]> CarregarXmlAutorizadoAsync(IHostEnvironment env)
    {
        var candidatos = new[]
        {
            Path.Combine(env.ContentRootPath, "Fixtures", "nfse-autorizada-sefin.xml"),
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "nfse-autorizada-sefin.xml"),
            Path.Combine(env.ContentRootPath, "..", "..", "tests", "Aluguel.Infrastructure.UnitTests", "Fixtures", "nfse-autorizada-sefin.xml"),
        };
        foreach (var path in candidatos)
        {
            if (File.Exists(path))
                return await File.ReadAllBytesAsync(path);
        }

        throw new FileNotFoundException(
            "Fixture nfse-autorizada-sefin.xml não encontrada para seed do DANFSe demo.");
    }

    private static string? ExtrairChave(byte[] xml)
    {
        var texto = Encoding.UTF8.GetString(xml);
        const string marker = "Id=\"NFS";
        var i = texto.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return null;
        var start = i + marker.Length;
        var end = texto.IndexOf('"', start);
        if (end <= start) return null;
        return texto[start..end];
    }
}
