using System.Globalization;

namespace Aluguel.Domain.Financeiro;

/// <summary>
/// Vencimento do boleto: dia cadastrado no mês seguinte à competência
/// (ex.: competência 10/2026 + dia 15 → 15/11/2026).
/// </summary>
public static class CobrancaVencimento
{
    public static DateOnly Calcular(string competencia, int dia)
    {
        if (string.IsNullOrWhiteSpace(competencia) || competencia.Length < 7)
            throw new ArgumentException("Competência inválida.", nameof(competencia));

        var mes = int.Parse(competencia.AsSpan(0, 2), CultureInfo.InvariantCulture);
        var ano = int.Parse(competencia.AsSpan(3, 4), CultureInfo.InvariantCulture);

        var mesVenc = mes == 12 ? 1 : mes + 1;
        var anoVenc = mes == 12 ? ano + 1 : ano;
        var diaOk = Math.Clamp(dia, 1, DateTime.DaysInMonth(anoVenc, mesVenc));
        return new DateOnly(anoVenc, mesVenc, diaOk);
    }
}
