using System.Globalization;
using System.Text;
using Aluguel.Application.Abstractions;

namespace Aluguel.Infrastructure.Integrations.BoletoBancario;

/// <summary>
/// PDF mínimo de boleto (texto) usado no ambiente Mock e como fallback visual.
/// Substitui-se pelo PDF oficial retornado pela API do banco em Homologação/Produção.
/// </summary>
internal static class BoletoPdfSimples
{
    public static byte[] Gerar(
        string provedorNome,
        string codigoBanco,
        BoletoGeracaoPedido pedido,
        string nossoNumero,
        string linhaDigitavel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("%PDF-1.4");
        sb.AppendLine("% Lucrare APP Aluguel — boleto");
        sb.AppendLine($"% Provedor: {provedorNome} (banco {codigoBanco})");
        sb.AppendLine($"% Beneficiário: {pedido.BeneficiarioNome} {pedido.BeneficiarioDocumento}");
        sb.AppendLine($"% Pagador: {pedido.PagadorNome} {pedido.PagadorDocumento}");
        sb.AppendLine($"% Competência: {pedido.Competencia}");
        sb.AppendLine($"% Valor: R$ {pedido.Valor.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"% Vencimento: {pedido.DataVencimento:dd/MM/yyyy}");
        sb.AppendLine($"% Nosso número: {nossoNumero}");
        sb.AppendLine($"% Linha digitável: {linhaDigitavel}");
        if (!string.IsNullOrWhiteSpace(pedido.Descricao))
            sb.AppendLine($"% Descrição: {pedido.Descricao}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Monta linha digitável ilustrativa com o código FEBRABAN do banco (posições 1–3).
    /// Em produção a linha vem da API do provedor.
    /// </summary>
    public static string LinhaDigitavelMock(string codigoBanco, long sequencial, decimal valor)
    {
        var banco = string.IsNullOrWhiteSpace(codigoBanco) ? "000" : codigoBanco.PadLeft(3, '0')[..3];
        var fator = sequencial % 1_000_000_000_000L;
        var valorCents = (long)Math.Round(valor * 100m, MidpointRounding.AwayFromZero);
        return $"{banco}9.{fator / 100_000_000:00000} " +
               $"{fator / 100_000 % 1000:00000}.{fator / 100 % 1000:000000} " +
               $"{fator % 100:00000}.{valorCents % 1_000_000:000000} " +
               $"{sequencial % 10} {valorCents}";
    }

    public static string NossoNumeroMock(string codigoBanco, long sequencial)
    {
        var prefixo = string.IsNullOrWhiteSpace(codigoBanco) ? "BOL" : codigoBanco.PadLeft(3, '0')[..3];
        return $"{prefixo}{sequencial % 100_000_000_000L:D11}";
    }
}
