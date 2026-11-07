using System.Globalization;
using System.Text;
using Aluguel.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Aluguel.Infrastructure.Integrations.PixInter;

/// <summary>Gera cobrança PIX fictícia em desenvolvimento (sem chamar o Banco Inter).</summary>
public sealed class MockInterPixProvider(IOptions<InterPixOptions> options) : IInterPixProvider
{
    public InterPixBeneficiario ObterBeneficiario()
    {
        var o = options.Value;
        return new InterPixBeneficiario(
            o.BeneficiarioNome,
            o.BeneficiarioCnpj,
            o.BeneficiarioBanco,
            o.BeneficiarioContaCorrente);
    }

    public Task<InterPixCobrancaResultado> CriarCobrancaImediataAsync(
        InterPixCobrancaPedido pedido, CancellationToken ct = default)
    {
        var valor = pedido.Valor.ToString("0.00", CultureInfo.InvariantCulture);
        var copiaCola = MontarPayloadMock(pedido.TxId, valor, options.Value.BeneficiarioNome);

        return Task.FromResult(new InterPixCobrancaResultado(
            Sucesso: true,
            TxId: pedido.TxId,
            PixCopiaCola: copiaCola,
            QrCodeBase64: null,
            Location: $"mock://pix/{pedido.TxId}",
            Status: "ATIVA"));
    }

    /// <summary>
    /// Payload EMV-like determinístico (não é BR Code válido para liquidação real —
    /// serve para exibir QR/copia-e-cola em ambiente Mock).
    /// </summary>
    internal static string MontarPayloadMock(string txId, string valor, string nomeBeneficiario)
    {
        var nome = new string(nomeBeneficiario.Where(char.IsLetterOrDigit).Take(25).ToArray());
        if (string.IsNullOrEmpty(nome)) nome = "TTHIMOTEO";

        var sb = new StringBuilder();
        sb.Append("000201");
        sb.Append("010212");
        sb.Append("26").Append((25 + txId.Length).ToString("00"));
        sb.Append("0014br.gov.bcb.pix");
        sb.Append("25").Append(txId.Length.ToString("00")).Append(txId);
        sb.Append("52040000");
        sb.Append("5303986");
        sb.Append("54").Append(valor.Length.ToString("00")).Append(valor);
        sb.Append("5802BR");
        sb.Append("59").Append(nome.Length.ToString("00")).Append(nome);
        sb.Append("6009SAO PAULO");
        sb.Append("62070503***");
        var semCrc = sb.ToString() + "6304";
        var crc = Crc16Ccitt(Encoding.ASCII.GetBytes(semCrc));
        return semCrc + crc.ToString("X4");
    }

    private static ushort Crc16Ccitt(byte[] data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc;
    }
}
