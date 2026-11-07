using Aluguel.Domain.Assinaturas;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class AssinaturaPixTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Cliente = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PlanoBasico = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid PlanoInter = Guid.Parse("11111111-1111-1111-1111-111111111103");

    [Fact]
    public void SolicitarContratacao_deixa_pendente_e_atualiza_plano()
    {
        var a = new Assinatura(Tenant, Cliente, PlanoBasico, 49.99m, StatusAssinatura.Trial);
        a.IniciarPrimeiraContratacao(PlanoInter, 149.99m);

        a.Status.Should().Be(StatusAssinatura.PendentePagamento);
        a.PlanoId.Should().Be(PlanoInter);
        a.ValorMensal.Should().Be(149.99m);
    }

    [Fact]
    public void CriarPagamentoPix_cancela_pendentes_anteriores()
    {
        var a = new Assinatura(Tenant, Cliente, PlanoBasico, 49.99m, StatusAssinatura.PendentePagamento);
        var tx1 = new string('a', 26);
        var tx2 = new string('b', 26);
        var p1 = a.CriarPagamentoPix(PlanoBasico, 49.99m, DateTimeOffset.UtcNow.AddHours(1), tx1);
        var p2 = a.CriarPagamentoPix(PlanoBasico, 49.99m, DateTimeOffset.UtcNow.AddHours(1), tx2);

        p1.Status.Should().Be(StatusPagamentoPlano.Falhou);
        p2.Status.Should().Be(StatusPagamentoPlano.Pendente);
        p2.PixTxId.Should().Be(tx2);
        p2.MetodoPagamento.Should().Be(MetodoPagamento.PIX);
        a.Pagamentos.Should().HaveCount(2);
    }

    [Fact]
    public void ConfirmarPagamentoPix_e_ativar_assinatura_sao_idempotentes()
    {
        var a = new Assinatura(Tenant, Cliente, PlanoBasico, 49.99m, StatusAssinatura.PendentePagamento);
        var tx = new string('c', 26);
        var p = a.CriarPagamentoPix(PlanoBasico, 49.99m, DateTimeOffset.UtcNow.AddHours(1), tx);
        p.RegistrarQrCode("00020126...");

        var agora = DateTimeOffset.Parse("2026-11-07T15:00:00Z");
        p.ConfirmarPagamentoPix("E1234567890123456789012345678901", agora);
        a.AtivarAposPagamento(agora, PlanoBasico, 49.99m);

        p.Status.Should().Be(StatusPagamentoPlano.Pago);
        p.PixEndToEndId.Should().Be("E1234567890123456789012345678901");
        a.Status.Should().Be(StatusAssinatura.Ativa);
        a.ProximaCobranca.Should().Be(agora.AddMonths(1));

        p.ConfirmarPagamentoPix("EOUTRO", agora.AddMinutes(1)); // idempotente
        p.PixEndToEndId.Should().Be("E1234567890123456789012345678901");
    }

    [Fact]
    public void CriarPix_exige_txid_valido()
    {
        var act = () => PagamentoPlano.CriarPix(
            Tenant, Guid.NewGuid(), PlanoBasico, 10m, DateTimeOffset.UtcNow, "curto");
        act.Should().Throw<ArgumentException>();
    }
}
