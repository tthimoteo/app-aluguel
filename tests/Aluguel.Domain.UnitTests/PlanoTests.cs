using Aluguel.Domain.Assinaturas;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class PlanoTests
{
    private static Plano Basico() =>
        new(Guid.NewGuid(), PlanoCodigo.Basico, "Básico", maxImoveis: 3, maxUsuarios: 1,
            permiteNfse: false, trialDias: 0, valorMensal: 49.99m);

    private static Plano Pro() =>
        new(Guid.NewGuid(), PlanoCodigo.Pro, "Pro", maxImoveis: null, maxUsuarios: null,
            permiteNfse: true, trialDias: 0, valorMensal: null);

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
    public void Plano_Basico_nao_permite_nfse()
    {
        Basico().PermiteNfse.Should().BeFalse();
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
}
