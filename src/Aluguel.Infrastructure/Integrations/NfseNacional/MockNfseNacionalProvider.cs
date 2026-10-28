using System.Globalization;
using System.Text;
using Aluguel.Application.Abstractions;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Emissão/cancelamento local simulados (CI / Ambient=Mock) — não chama a SEFIN.</summary>
public sealed class MockNfseNacionalProvider : INfseNacionalProvider
{
    public Task<NfseEmissaoResultado> EmitirAsync(NfseEmissaoPedido pedido, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(pedido);

        var numero = Math.Abs(BitConverter.ToInt64(pedido.NfseId.ToByteArray(), 0) % 900_000) + 100_000;
        var chave = $"MOCK{numero}{pedido.Competencia.Replace("/", "", StringComparison.Ordinal)}";
        var xml = Encoding.UTF8.GetBytes(
            $"""<?xml version="1.0" encoding="UTF-8"?><NFSe xmlns="http://www.sped.fazenda.gov.br/nfse"><Numero>{numero}</Numero><Competencia>{pedido.Competencia}</Competencia><Valor>{pedido.ValorServico.ToString("F2", CultureInfo.InvariantCulture)}</Valor><Chave>{chave}</Chave></NFSe>""");

        return Task.FromResult(new NfseEmissaoResultado(
            Sucesso: true,
            Numero: numero,
            Serie: "1",
            ChaveAcesso: chave,
            IdDps: $"MOCK-DPS-{numero}",
            XmlNfse: xml,
            XmlDpsEnviado: null,
            MensagemErro: null));
    }

    public Task<NfseCancelamentoResultado> CancelarAsync(
        NfseCancelamentoPedido pedido, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(pedido);

        var chave = new string((pedido.ChaveAcesso ?? "").Where(char.IsDigit).ToArray());
        var prot = $"MOCK-CANC-{chave[^Math.Min(8, Math.Max(chave.Length, 1))..]}";
        var xml = Encoding.UTF8.GetBytes(
            $"""<?xml version="1.0" encoding="UTF-8"?><evento xmlns="http://www.sped.fazenda.gov.br/nfse"><infEvento><chNFSe>{pedido.ChaveAcesso}</chNFSe><nProt>{prot}</nProt><tpEvento>e101101</tpEvento></infEvento></evento>""");

        return Task.FromResult(new NfseCancelamentoResultado(
            Sucesso: true,
            Protocolo: prot,
            XmlEvento: xml,
            MensagemErro: null));
    }
}
