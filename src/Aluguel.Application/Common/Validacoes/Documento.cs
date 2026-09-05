namespace Aluguel.Application.Common.Validacoes;

/// <summary>Validação de CPF/CNPJ (somente dígitos) com verificação dos dígitos verificadores.</summary>
public static class Documento
{
    public static bool CpfValido(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11 || !cpf.All(char.IsDigit))
            return false;
        if (cpf.Distinct().Count() == 1)
            return false;

        var numeros = cpf.Select(c => c - '0').ToArray();

        var d1 = DigitoVerificador(numeros, 9, 10);
        var d2 = DigitoVerificador(numeros, 10, 11);
        return d1 == numeros[9] && d2 == numeros[10];
    }

    public static bool CnpjValido(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14 || !cnpj.All(char.IsDigit))
            return false;
        if (cnpj.Distinct().Count() == 1)
            return false;

        var numeros = cnpj.Select(c => c - '0').ToArray();
        int[] pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var d1 = DigitoCnpj(numeros, pesos1, 12);
        var d2 = DigitoCnpj(numeros, pesos2, 13);
        return d1 == numeros[12] && d2 == numeros[13];
    }

    private static int DigitoVerificador(int[] numeros, int quantidade, int pesoInicial)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
            soma += numeros[i] * (pesoInicial - i);
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static int DigitoCnpj(int[] numeros, int[] pesos, int quantidade)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
            soma += numeros[i] * pesos[i];
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    /// <summary>Remove máscara (pontos, traços, barras, espaços) deixando apenas dígitos.</summary>
    public static string? SomenteDigitos(string? valor) =>
        valor is null ? null : new string(valor.Where(char.IsDigit).ToArray());
}
