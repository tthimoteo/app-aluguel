using System.Reflection;
using Aluguel.Infrastructure.Integrations.NfseNacional;
using FluentAssertions;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

/// <summary>
/// Garante que o parser de erros SEFIN aceita "erro"/"erros" como objeto ou array
/// (resposta típica de eventos: <c>"erro":[]</c> ou <c>"erro":[{...}]</c>).
/// </summary>
public class SefinErroJsonParserTests
{
    private static string? Extrair(string body)
    {
        var mi = typeof(SefinNfseNacionalProvider).GetMethod(
            "ExtrairMensagemErro", BindingFlags.NonPublic | BindingFlags.Static);
        mi.Should().NotBeNull();
        return (string?)mi!.Invoke(null, [body]);
    }

    [Fact]
    public void Erro_array_com_itens_nao_lanca()
    {
        var msg = Extrair("""{"erro":[{"Codigo":"E0004","Descricao":"Id inválido","Complemento":"x"}]}""");
        msg.Should().Contain("E0004");
        msg.Should().Contain("Id inválido");
    }

    [Fact]
    public void Erro_array_vazio_retorna_null()
    {
        Extrair("""{"tipoAmbiente":2,"erro":[]}""").Should().BeNull();
    }

    [Fact]
    public void Erros_plural_array_ok()
    {
        var msg = Extrair("""{"erros":[{"codigo":"E1101","descricao":"Prazo expirado"}]}""");
        msg.Should().Contain("E1101");
    }

    [Fact]
    public void Erro_objeto_unico_ok()
    {
        var msg = Extrair("""{"erro":{"Codigo":"E1102","Descricao":"Já cancelada"}}""");
        msg.Should().Contain("E1102");
    }
}
