using System.Text;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Cobrancas.RegistrarPagamentoCobranca;
using Aluguel.Application.Cobrancas.RemoverCobranca;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class CobrancaPagamentoRemocaoTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");
    private static readonly Guid ImovelId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private sealed record Cenario(
        FakeCobrancaRepository Cobrancas,
        FakeClienteRepository Clientes,
        FakePlanoRepository Planos,
        FakeCurrentUser User,
        Cliente Cliente,
        NotaFiscalServico Nfse,
        Boleto Boleto,
        FakeNfseNacionalProvider NfseProvider,
        FakeFileStorage Storage,
        FakeSecretProtector Protector,
        FakeCertificadoDigitalRepository Certificados,
        FakeTimeProvider Relogio);

    private static Cenario Montar(
        bool comPagamento = false,
        bool nfseEmitida = true,
        DateTimeOffset? emitidaEm = null)
    {
        var plano = new Plano(Guid.NewGuid(), PlanoCodigo.Avancado, "Avançado", 10, 5, true, 0, 249.99m);
        var cliente = Cliente.CriarPessoaJuridica(
            Tenant, plano.Id, "Locadora Demo LTDA", "Locadora",
            "15809930000168", "12345", "8219999", null, "gestor@demo.local", null);

        var clientes = new FakeClienteRepository();
        clientes.Adicionar(cliente);
        var planos = new FakePlanoRepository();
        planos.Itens.Add(plano);

        var cobrancas = new FakeCobrancaRepository();
        var nfse = new NotaFiscalServico(Tenant, cliente.Id, ImovelId, "10/2026", 2500m);
        if (nfseEmitida)
        {
            var quando = emitidaEm ?? DateTimeOffset.UtcNow;
            nfse.MarcarEmitida(1, "1", "35503082215809930000168000000000000126108889952559", Guid.NewGuid(), quando);
        }

        cobrancas.AdicionarNfse(nfse);

        var boleto = new Boleto(Tenant, cliente.Id, ImovelId, "10/2026", 2500m,
            new DateOnly(2026, 10, 10), nfse.Id, "BOL1", "23790");
        cobrancas.AdicionarBoleto(boleto);

        if (comPagamento)
        {
            cobrancas.AdicionarPagamento(new Pagamento(Tenant, cliente.Id, ImovelId, "10/2026", 2500m,
                new DateOnly(2026, 10, 12), nfseId: nfse.Id));
            boleto.MarcarPago();
        }

        var certs = new FakeCertificadoDigitalRepository();
        var senhaCifrada = Encoding.UTF8.GetBytes("senha");
        certs.Adicionar(new CertificadoDigital(
            Tenant, cliente.Id, "certs/demo.pfx", "THUMBDEMO",
            DateTimeOffset.UtcNow.AddYears(1), senhaCifrada));
        var storage = new FakeFileStorage();
        storage.Arquivos["certificates/certs/demo.pfx"] = [1, 2, 3, 4];
        var protector = new FakeSecretProtector();
        var nfseProvider = new FakeNfseNacionalProvider();
        var relogio = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-28T15:00:00Z"));

        var user = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        return new Cenario(cobrancas, clientes, planos, user, cliente, nfse, boleto,
            nfseProvider, storage, protector, certs, relogio);
    }

    private static RemoverCobrancaCommandHandler Handler(Cenario c) =>
        new(c.Cobrancas, c.Clientes, c.Planos, c.Certificados, c.Storage, c.Protector,
            c.NfseProvider, c.User, c.Relogio);

    [Fact]
    public async Task Registrar_pagamento_cria_pagamento_e_marca_boleto()
    {
        var c = Montar();
        var handler = new RegistrarPagamentoCobrancaCommandHandler(
            c.Cobrancas, c.Clientes, c.Planos, c.User);

        var dto = await handler.Handle(
            new RegistrarPagamentoCobrancaCommand(c.Nfse.Id, new DateOnly(2026, 10, 15), 2500m),
            default);

        dto.TemPagamento.Should().BeTrue();
        dto.ValorPago.Should().Be(2500m);
        dto.DataPagamento.Should().Be(new DateOnly(2026, 10, 15));
        dto.StatusBoleto.Should().Be(nameof(StatusBoleto.Pago));
        c.Cobrancas.Pagamentos.Should().HaveCount(1);
        c.Boleto.Status.Should().Be(StatusBoleto.Pago);
        c.Cobrancas.SalvouVezes.Should().Be(1);
    }

    [Fact]
    public async Task Registrar_pagamento_ja_pago_lanca()
    {
        var c = Montar(comPagamento: true);
        var handler = new RegistrarPagamentoCobrancaCommandHandler(
            c.Cobrancas, c.Clientes, c.Planos, c.User);

        await handler.Invoking(h => h.Handle(new RegistrarPagamentoCobrancaCommand(c.Nfse.Id), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já está marcada como paga*");
    }

    [Fact]
    public async Task Remover_cobranca_sem_nfse_emitida_exclui_registro()
    {
        var c = Montar(nfseEmitida: false);
        var ok = await Handler(c).Handle(new RemoverCobrancaCommand(c.Nfse.Id), default);

        ok!.Removida.Should().BeTrue();
        ok.CancelamentoNfse.Should().Be(NfseCancelamentoAoRemover.SemNfseEmitida);
        c.Cobrancas.Nfses.Should().BeEmpty();
        c.Cobrancas.Boletos.Should().BeEmpty();
        c.NfseProvider.Cancelamentos.Should().BeEmpty();
    }

    [Fact]
    public async Task Remover_cobranca_emitida_ha_menos_de_24h_cancela_na_sefin()
    {
        var c = Montar(emitidaEm: DateTimeOffset.Parse("2026-10-28T10:00:00Z"));
        var ok = await Handler(c).Handle(new RemoverCobrancaCommand(c.Nfse.Id), default);

        ok!.Removida.Should().BeTrue();
        ok.CancelamentoNfse.Should().Be(NfseCancelamentoAoRemover.CanceladaAutomaticamente);
        ok.Mensagem.Should().Contain("24 horas");
        c.Nfse.Status.Should().Be(StatusNfse.Cancelada);
        c.Nfse.ProtocoloCancelamento.Should().NotBeNullOrWhiteSpace();
        c.Cobrancas.Boletos.Should().BeEmpty();
        c.Cobrancas.Nfses.Should().ContainSingle(); // mantém histórico cancelado
        c.NfseProvider.Cancelamentos.Should().HaveCount(1);
        c.Cobrancas.Documentos.Should().Contain(d => d.Tipo == TipoDocumentoFiscal.XMLCancelamento);
    }

    [Fact]
    public async Task Remover_cobranca_emitida_ha_mais_de_24h_exige_cancelamento_manual()
    {
        var c = Montar(emitidaEm: DateTimeOffset.Parse("2026-10-20T10:00:00Z"));
        var ok = await Handler(c).Handle(new RemoverCobrancaCommand(c.Nfse.Id), default);

        ok!.Removida.Should().BeTrue();
        ok.CancelamentoNfse.Should().Be(NfseCancelamentoAoRemover.RequerCancelamentoManual);
        ok.Mensagem.Should().Contain("portal nacional");
        c.Nfse.Status.Should().Be(StatusNfse.Cancelada);
        c.Cobrancas.Boletos.Should().BeEmpty();
        c.NfseProvider.Cancelamentos.Should().BeEmpty();
    }

    [Fact]
    public async Task Remover_cobranca_paga_bloqueia()
    {
        var c = Montar(comPagamento: true);
        await Handler(c).Invoking(h => h.Handle(new RemoverCobrancaCommand(c.Nfse.Id), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já paga*");

        c.Cobrancas.Nfses.Should().HaveCount(1);
        c.Cobrancas.SalvouVezes.Should().Be(0);
    }

    [Fact]
    public async Task Remover_inexistente_retorna_null()
    {
        var c = Montar();
        var ok = await Handler(c).Handle(new RemoverCobrancaCommand(Guid.NewGuid()), default);
        ok.Should().BeNull();
    }

    [Fact]
    public async Task Remover_dentro_do_prazo_com_falha_sefin_nao_remove()
    {
        var c = Montar(emitidaEm: DateTimeOffset.Parse("2026-10-28T10:00:00Z"));
        c.NfseProvider.FalharCancelamento = "E1101 — Prazo expirado no município";

        await Handler(c).Invoking(h => h.Handle(new RemoverCobrancaCommand(c.Nfse.Id), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*E1101*");

        c.Nfse.Status.Should().Be(StatusNfse.Emitida);
        c.Cobrancas.Boletos.Should().HaveCount(1);
        c.Cobrancas.SalvouVezes.Should().Be(0);
    }
}

internal sealed class FakeNfseNacionalProvider : INfseNacionalProvider
{
    public List<NfseCancelamentoPedido> Cancelamentos { get; } = [];
    public string? FalharCancelamento { get; set; }

    public Task<NfseEmissaoResultado> EmitirAsync(NfseEmissaoPedido pedido, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<NfseCancelamentoResultado> CancelarAsync(
        NfseCancelamentoPedido pedido, CancellationToken ct = default)
    {
        Cancelamentos.Add(pedido);
        if (FalharCancelamento is { } msg)
            return Task.FromResult(new NfseCancelamentoResultado(false, null, null, msg));

        var xml = Encoding.UTF8.GetBytes($"<evento><nProt>PROT-{Cancelamentos.Count}</nProt></evento>");
        return Task.FromResult(new NfseCancelamentoResultado(true, $"PROT-{Cancelamentos.Count}", xml, null));
    }
}

internal sealed class FakeFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Arquivos { get; } = new(StringComparer.Ordinal);

    public Task<string> UploadAsync(string bucket, string path, Stream conteudo, string contentType,
        CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        conteudo.CopyTo(ms);
        Arquivos[$"{bucket}/{path}"] = ms.ToArray();
        return Task.FromResult(path);
    }

    public Task<Stream> DownloadAsync(string bucket, string path, CancellationToken ct = default)
    {
        if (!Arquivos.TryGetValue($"{bucket}/{path}", out var bytes))
            throw new FileNotFoundException(path);
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string bucket, string path, CancellationToken ct = default)
    {
        Arquivos.Remove($"{bucket}/{path}");
        return Task.CompletedTask;
    }
}

internal sealed class FakeSecretProtector : ISecretProtector
{
    public byte[] Protect(string plain) => Encoding.UTF8.GetBytes(plain);
    public string Unprotect(byte[] protectedBytes) => Encoding.UTF8.GetString(protectedBytes);
}

internal sealed class FakeCertificadoDigitalRepository : ICertificadoDigitalRepository
{
    private readonly List<CertificadoDigital> _itens = [];

    public Task<CertificadoDigital?> ObterAtivoPorClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        Task.FromResult(_itens.FirstOrDefault(c => c.ClienteId == clienteId && c.Ativo));

    public Task<IReadOnlyList<CertificadoDigital>> ListarAtivosPorClienteAsync(
        Guid clienteId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CertificadoDigital>>(
            _itens.Where(c => c.ClienteId == clienteId && c.Ativo).ToList());

    public void Adicionar(CertificadoDigital certificado) => _itens.Add(certificado);
    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => Task.FromResult(0);
}

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
