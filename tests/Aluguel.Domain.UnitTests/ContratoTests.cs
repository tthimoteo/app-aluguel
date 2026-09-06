using Aluguel.Domain.Contratos;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class ContratoTests
{
    private static Contrato NovoAtivo() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "CT-001",
        new DateOnly(2026, 1, 1), diaVencimento: 10, valorAluguel: 2500m,
        dataFimPrevista: new DateOnly(2026, 12, 31));

    [Fact]
    public void Novo_contrato_nasce_ativo()
    {
        NovoAtivo().Status.Should().Be(StatusContrato.Ativo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Dia_vencimento_fora_do_intervalo_lanca(int dia)
    {
        var act = () => new Contrato(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "CT-001", new DateOnly(2026, 1, 1), dia, 1000m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Valor_nao_positivo_lanca()
    {
        var act = () => new Contrato(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "CT-001", new DateOnly(2026, 1, 1), 10, 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Data_fim_anterior_ao_inicio_lanca()
    {
        var act = () => new Contrato(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "CT-001", new DateOnly(2026, 6, 1), 10, 1000m, dataFimPrevista: new DateOnly(2026, 1, 1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encerrar_muda_status_para_encerrado()
    {
        var c = NovoAtivo();
        c.Encerrar();
        c.Status.Should().Be(StatusContrato.Encerrado);
    }

    [Fact]
    public void Cancelar_muda_status_para_cancelado()
    {
        var c = NovoAtivo();
        c.Cancelar();
        c.Status.Should().Be(StatusContrato.Cancelado);
    }

    [Fact]
    public void Encerrar_contrato_nao_ativo_lanca()
    {
        var c = NovoAtivo();
        c.Encerrar();
        c.Invoking(x => x.Encerrar()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancelar_contrato_nao_ativo_lanca()
    {
        var c = NovoAtivo();
        c.Cancelar();
        c.Invoking(x => x.Encerrar()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Atualizar_contrato_nao_ativo_lanca()
    {
        var c = NovoAtivo();
        c.Encerrar();
        c.Invoking(x => x.Atualizar(null, 5, 3000m, null, null, null))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Atualizar_ativo_aplica_alteracoes()
    {
        var c = NovoAtivo();
        c.Atualizar(new DateOnly(2027, 1, 1), 15, 3200m, 1m, 2m, "contracts/ct-001.pdf");

        c.DiaVencimento.Should().Be(15);
        c.ValorAluguel.Should().Be(3200m);
        c.MultaAtrasoPct.Should().Be(2m);
        c.AnexoPath.Should().Be("contracts/ct-001.pdf");
    }
}
