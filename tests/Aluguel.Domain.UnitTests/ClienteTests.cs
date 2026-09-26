using Aluguel.Domain.Clientes;
using Aluguel.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class ClienteTests
{
    [Fact]
    public void DefinirDadosFiscaisNfse_persiste_cnae_servico_e_regime_em_pf()
    {
        var c = Cliente.CriarPessoaFisica(
            Guid.NewGuid(), null, "Locador Demo", "39053344705",
            null, null, null, Endereco.Vazio());

        c.DefinirDadosFiscaisNfse("6201501", "07.02", RegimeTributario.SimplesNacional);

        c.CnaePrincipal.Should().Be("6201501");
        c.CodigoServico.Should().Be("07.02");
        c.RegimeTributario.Should().Be(RegimeTributario.SimplesNacional);
    }

    [Fact]
    public void DefinirDadosFiscaisNfse_limpa_campos_em_branco()
    {
        var c = Cliente.CriarPessoaJuridica(
            Guid.NewGuid(), null, "ACME LTDA", null, "11222333000181",
            null, "6201501", null, null, Endereco.Vazio());

        c.DefinirDadosFiscaisNfse("  ", "  ", RegimeTributario.LucroPresumido);

        c.CnaePrincipal.Should().BeNull();
        c.CodigoServico.Should().BeNull();
        c.RegimeTributario.Should().Be(RegimeTributario.LucroPresumido);
    }

    [Fact]
    public void DefinirPlano_atualiza_plano_id()
    {
        var c = Cliente.CriarPessoaFisica(
            Guid.NewGuid(), null, "Locador Demo", "39053344705",
            null, null, null, Endereco.Vazio());
        var plano = Guid.NewGuid();

        c.DefinirPlano(plano);

        c.PlanoId.Should().Be(plano);
    }
}
