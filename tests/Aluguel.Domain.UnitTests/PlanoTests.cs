using Aluguel.Domain.Assinaturas;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class PlanoTests
{
    private static Plano Basico() =>
        new(Guid.NewGuid(), PlanoCodigo.Basico, "Básico", maxImoveis: 3, maxUsuarios: 1,
            permiteNfse: true, trialDias: 0, valorMensal: 49.99m, permiteBoleto: false);

    private static Plano Intermediario() =>
        new(Guid.NewGuid(), PlanoCodigo.Intermediario, "Intermediário", maxImoveis: 5, maxUsuarios: 3,
            permiteNfse: true, trialDias: 0, valorMensal: 149.99m, permiteBoleto: false);

    private static Plano Avancado() =>
        new(Guid.NewGuid(), PlanoCodigo.Avancado, "Avançado", maxImoveis: 10, maxUsuarios: 5,
            permiteNfse: true, trialDias: 0, valorMensal: 249.99m, permiteBoleto: true);

    private static Plano Pro() =>
        new(Guid.NewGuid(), PlanoCodigo.Pro, "Pro", maxImoveis: null, maxUsuarios: null,
            permiteNfse: true, trialDias: 0, valorMensal: null, permiteBoleto: true);

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void PermiteMaisImoveis_respeita_limite_do_plano(int totalAtual, bool esperado) =>
        Basico().PermiteMaisImoveis(totalAtual).Should().Be(esperado);

    [Fact]
    public void Plano_Pro_nao_tem_limite_de_imoveis()
    {
        var pro = Pro();
        pro.PermiteMaisImoveis(1000).Should().BeTrue();
        pro.MaxImoveis.Should().BeNull();
    }

    [Fact]
    public void Plano_Basico_e_Intermediario_permitem_nfse_sem_boleto()
    {
        Basico().PermiteNfse.Should().BeTrue();
        Basico().PermiteBoleto.Should().BeFalse();
        Intermediario().PermiteNfse.Should().BeTrue();
        Intermediario().PermiteBoleto.Should().BeFalse();
    }

    [Fact]
    public void Plano_Avancado_e_Pro_permitem_nfse_e_boleto()
    {
        Avancado().PermiteNfse.Should().BeTrue();
        Avancado().PermiteBoleto.Should().BeTrue();
        Pro().PermiteNfse.Should().BeTrue();
        Pro().PermiteBoleto.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    public void PermiteMaisUsuarios_respeita_limite_do_plano(int totalAtual, bool esperado) =>
        Basico().PermiteMaisUsuarios(totalAtual).Should().Be(esperado);

    [Fact]
    public void Plano_Pro_nao_tem_limite_de_usuarios()
    {
        var pro = Pro();
        pro.PermiteMaisUsuarios(1000).Should().BeTrue();
        pro.MaxUsuarios.Should().BeNull();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void SuportaUtilizacaoImoveis_para_downgrade(int totalAtual, bool esperado) =>
        Basico().SuportaUtilizacaoImoveis(totalAtual).Should().Be(esperado);

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void SuportaUtilizacaoUsuarios_para_downgrade(int totalAtual, bool esperado) =>
        Basico().SuportaUtilizacaoUsuarios(totalAtual).Should().Be(esperado);

    [Fact]
    public void PermiteModuloCobranca_segue_flag_de_nfse()
    {
        Basico().PermiteModuloCobranca.Should().BeTrue();
        Intermediario().PermiteModuloCobranca.Should().BeTrue();
        Avancado().PermiteModuloCobranca.Should().BeTrue();
        Pro().PermiteModuloCobranca.Should().BeTrue();
        new Plano(Guid.NewGuid(), PlanoCodigo.Trial, "Trial", 1, 1, false, 7, 0m)
            .PermiteModuloCobranca.Should().BeFalse();
    }
}
