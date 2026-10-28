using Aluguel.Application.Abstractions;
using Aluguel.Infrastructure.Integrations.BoletoBancario;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

public class BoletoProviderFactoryTests
{
    private static BoletoProviderFactory Factory()
    {
        var opts = Options.Create(new BoletoOptions { Ambiente = "Mock" });
        var http = new FakeHttpClientFactory();
        IBoletoProvider[] providers =
        [
            new ItauBoletoProvider(http, opts, NullLogger<ItauBoletoProvider>.Instance),
            new BradescoBoletoProvider(http, opts, NullLogger<BradescoBoletoProvider>.Instance),
            new BancoDoBrasilBoletoProvider(http, opts, NullLogger<BancoDoBrasilBoletoProvider>.Instance),
            new SantanderBoletoProvider(http, opts, NullLogger<SantanderBoletoProvider>.Instance),
            new CaixaBoletoProvider(http, opts, NullLogger<CaixaBoletoProvider>.Instance),
            new SicrediBoletoProvider(http, opts, NullLogger<SicrediBoletoProvider>.Instance),
            new MercadoPagoBoletoProvider(http, opts, NullLogger<MercadoPagoBoletoProvider>.Instance),
            new InterBoletoProvider(http, opts, NullLogger<InterBoletoProvider>.Instance),
            new C6BoletoProvider(http, opts, NullLogger<C6BoletoProvider>.Instance),
            new NubankBoletoProvider(http, opts, NullLogger<NubankBoletoProvider>.Instance),
        ];
        return new BoletoProviderFactory(providers);
    }

    [Theory]
    [InlineData("itau", "341")]
    [InlineData("bradesco", "237")]
    [InlineData("nubank", "260")]
    [InlineData("mercado_pago", "")]
    public async Task Mock_gera_pdf_e_linha_com_codigo_do_banco(string codigo, string febraban)
    {
        var provider = Factory().Obter(codigo);
        var pedido = new BoletoGeracaoPedido(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Guid.NewGuid(),
            "10/2026",
            1500.50m,
            new DateOnly(2026, 11, 10),
            "Locador SA",
            "12345678000199",
            true,
            "Inquilino",
            "12345678901",
            false,
            "Aluguel 10/2026",
            new Dictionary<string, string> { ["client_id"] = "x", ["client_secret"] = "y" });

        var r = await provider.GerarAsync(pedido);

        r.Sucesso.Should().BeTrue();
        r.PdfBytes.Should().NotBeNullOrEmpty();
        r.NossoNumero.Should().NotBeNullOrWhiteSpace();
        r.LinhaDigitavel.Should().NotBeNullOrWhiteSpace();
        if (!string.IsNullOrEmpty(febraban))
            r.LinhaDigitavel!.Should().StartWith(febraban);
        r.IdExterno.Should().Contain(codigo);
    }

    [Fact]
    public void Obter_provedor_desconhecido_falha()
    {
        var act = () => Factory().Obter("banco_xyz");
        act.Should().Throw<InvalidOperationException>().WithMessage("*não está registrado*");
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
