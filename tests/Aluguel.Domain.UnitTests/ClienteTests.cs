using Aluguel.Domain.Clientes;
using Aluguel.Domain.Fiscal;
using Aluguel.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class ClienteTests
{
    [Fact]
    public void Cliente_nasce_ativo()
    {
        var c = Cliente.CriarPessoaFisica(Guid.NewGuid(), null, "João", "39053344705", null, null, null, null);
        c.Status.Should().Be(StatusCliente.Ativo);
    }

    [Fact]
    public void DefinirStatus_aceita_ativo_inativo_bloqueado()
    {
        var c = Cliente.CriarPessoaFisica(Guid.NewGuid(), null, "João", "39053344705", null, null, null, null);
        c.DefinirStatus(StatusCliente.Inativo);
        c.Status.Should().Be(StatusCliente.Inativo);
        c.DefinirStatus(StatusCliente.Bloqueado);
        c.Status.Should().Be(StatusCliente.Bloqueado);
        c.DefinirStatus(StatusCliente.Ativo);
        c.Status.Should().Be(StatusCliente.Ativo);
    }

    [Fact]
    public void DefinirDadosFiscaisNfse_persiste_codigos_lucrare_em_pf()
    {
        var c = Cliente.CriarPessoaFisica(
            Guid.NewGuid(), null, "Locador Demo", "39053344705",
            null, null, null, Endereco.Vazio());

        c.DefinirDadosFiscaisNfse(
            "6201501",
            NfseCodigosPadrao.CodigoTributacaoNacional,
            RegimeTributario.SimplesNacional,
            NfseCodigosPadrao.CodigoIndicadorOperacao,
            NfseCodigosPadrao.ClassificacaoTributaria);

        c.CnaePrincipal.Should().Be("6201501");
        c.CodigoServico.Should().Be(NfseCodigosPadrao.CodigoTributacaoNacional);
        c.CodigoIndicadorOperacao.Should().Be(NfseCodigosPadrao.CodigoIndicadorOperacao);
        c.ClassificacaoTributaria.Should().Be(NfseCodigosPadrao.ClassificacaoTributaria);
        c.RegimeTributario.Should().Be(RegimeTributario.SimplesNacional);
    }

    [Fact]
    public void DefinirDadosFiscaisNfse_normaliza_somente_digitos()
    {
        var c = Cliente.CriarPessoaJuridica(
            Guid.NewGuid(), null, "ACME LTDA", null, "11222333000181",
            null, "6201501", null, null, Endereco.Vazio());

        c.DefinirDadosFiscaisNfse(
            "82.19.9-99", "990.301", RegimeTributario.SimplesNacional, "020.201", "200.027");

        c.CodigoServico.Should().Be("990301");
        c.CodigoIndicadorOperacao.Should().Be("020201");
        c.ClassificacaoTributaria.Should().Be("200027");
    }

    [Fact]
    public void DefinirDadosFiscaisNfse_limpa_campos_em_branco()
    {
        var c = Cliente.CriarPessoaJuridica(
            Guid.NewGuid(), null, "ACME LTDA", null, "11222333000181",
            null, "6201501", null, null, Endereco.Vazio());

        c.DefinirDadosFiscaisNfse(
            "  ", "  ", RegimeTributario.LucroPresumido, "  ", "  ");

        c.CnaePrincipal.Should().BeNull();
        c.CodigoServico.Should().BeNull();
        c.CodigoIndicadorOperacao.Should().BeNull();
        c.ClassificacaoTributaria.Should().BeNull();
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

    [Fact]
    public void ConverterParaPessoaJuridica_troca_pf_por_cnpj_e_razao()
    {
        var c = Cliente.CriarPessoaFisica(
            Guid.NewGuid(), null, "Cliente Demo", "39053344705",
            null, "11999990000", "gestor@demo.local", Endereco.Vazio());

        c.ConverterParaPessoaJuridica(
            "T THIMOTEO CONSULTORIA E SERVICOS DE ENGENHARIA LTDA",
            "Cliente Demo",
            "15.809.930/0001-68",
            cnaePrincipal: "82.19.9-99");

        c.TipoPessoa.Should().Be(TipoPessoa.PJ);
        c.Cnpj.Should().Be("15809930000168");
        c.RazaoSocial.Should().Be("T THIMOTEO CONSULTORIA E SERVICOS DE ENGENHARIA LTDA");
        c.NomeFantasia.Should().Be("Cliente Demo");
        c.Cpf.Should().BeNull();
        c.Nome.Should().BeNull();
        c.NomeExibicao.Should().Be("T THIMOTEO CONSULTORIA E SERVICOS DE ENGENHARIA LTDA");
    }
}
