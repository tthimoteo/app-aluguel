using Aluguel.Domain.Assinaturas;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class AssinaturaCicloVidaTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Cliente = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Basico = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid Inter = Guid.Parse("11111111-1111-1111-1111-111111111103");
    private static readonly Guid Avancado = Guid.Parse("11111111-1111-1111-1111-111111111104");

    [Fact]
    public void Upgrade_limpa_downgrade_e_cancelamento_pendentes()
    {
        var a = AssinaturaAtiva(Avancado, 249.99m);
        a.AgendarDowngrade(Inter);
        var agora = DateTimeOffset.Parse("2026-11-07T12:00:00Z");
        a.SolicitarCancelamento(agora);
        a.CancelamentoPendente.Should().BeTrue();
        a.PlanoAgendadoId.Should().BeNull(); // cancelamento limpa downgrade

        a.RevogarCancelamento();
        a.RegistrarIntencaoUpgrade();
        a.CancelamentoSolicitadoEm.Should().BeNull();
        a.PlanoAgendadoId.Should().BeNull();
    }

    [Fact]
    public void AgendarDowngrade_mesmo_plano_falha()
    {
        var a = AssinaturaAtiva(Basico, 49.99m);
        var act = () => a.AgendarDowngrade(Basico);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Downgrade_agendado_nao_altera_plano_vigente()
    {
        var a = AssinaturaAtiva(Avancado, 249.99m);
        a.AgendarDowngrade(Inter);

        a.PlanoId.Should().Be(Avancado);
        a.ValorMensal.Should().Be(249.99m);
        a.PlanoAgendadoId.Should().Be(Inter);
        a.Status.Should().Be(StatusAssinatura.Ativa);
        a.DowngradeAgendado.Should().BeTrue();
    }

    [Fact]
    public void Cancelamento_mantem_ativa_ate_fim_do_ciclo()
    {
        var a = AssinaturaAtiva(Inter, 149.99m);
        var agora = DateTimeOffset.Parse("2026-11-07T12:00:00Z");
        a.SolicitarCancelamento(agora);

        a.Status.Should().Be(StatusAssinatura.Ativa);
        a.CancelamentoPendente.Should().BeTrue();
        a.PlanoAgendadoId.Should().BeNull();

        a.EfetivarCancelamentoAoFimDoCiclo(agora.AddDays(20));
        a.Status.Should().Be(StatusAssinatura.Cancelada);
        a.CanceladaEm.Should().NotBeNull();
        a.ProximaCobranca.Should().BeNull();
    }

    [Fact]
    public void AtivarAposPagamento_reinicia_ciclo()
    {
        var a = AssinaturaAtiva(Basico, 49.99m);
        a.AgendarDowngrade(Inter); // será limpo no pagamento de upgrade
        var agora = DateTimeOffset.Parse("2026-11-07T12:00:00Z");
        a.RegistrarIntencaoUpgrade();
        a.AtivarAposPagamento(agora, Avancado, 249.99m);

        a.PlanoId.Should().Be(Avancado);
        a.ValorMensal.Should().Be(249.99m);
        a.PlanoAgendadoId.Should().BeNull();
        a.ProximaCobranca.Should().Be(agora.AddMonths(1));
        a.FimCicloPago.Should().Be(agora.AddMonths(1));
    }

    [Fact]
    public void PlanoRanking_classifica_upgrade_e_downgrade()
    {
        PlanoRanking.EhUpgrade(PlanoCodigo.Basico, PlanoCodigo.Avancado).Should().BeTrue();
        PlanoRanking.EhDowngrade(PlanoCodigo.Avancado, PlanoCodigo.Intermediario).Should().BeTrue();
        PlanoRanking.MesmoNivel(PlanoCodigo.Basico, PlanoCodigo.Basico).Should().BeTrue();
    }

    private static Assinatura AssinaturaAtiva(Guid planoId, decimal valor)
    {
        var a = new Assinatura(Tenant, Cliente, planoId, valor, StatusAssinatura.PendentePagamento);
        a.AtivarAposPagamento(DateTimeOffset.Parse("2026-10-07T12:00:00Z"), planoId, valor);
        return a;
    }
}
