using Aluguel.Application.Abstractions;

namespace Aluguel.Infrastructure.Integrations.BoletoBancario;

/// <summary>
/// Factory expansível: resolve o adapter pelo código do catálogo.
/// Novos bancos: registrar o provider no DI e incluir em <see cref="_porCodigo"/>.
/// </summary>
public sealed class BoletoProviderFactory(IEnumerable<IBoletoProvider> providers) : IBoletoProviderFactory
{
    private readonly IReadOnlyDictionary<string, IBoletoProvider> _porCodigo =
        providers.ToDictionary(p => p.CodigoProvedor, StringComparer.OrdinalIgnoreCase);

    public IBoletoProvider Obter(string codigoProvedor)
    {
        if (string.IsNullOrWhiteSpace(codigoProvedor))
            throw new InvalidOperationException("Código do provedor de boleto não informado.");

        var codigo = codigoProvedor.Trim().ToLowerInvariant().Replace(' ', '_');
        if (_porCodigo.TryGetValue(codigo, out var provider))
            return provider;

        throw new InvalidOperationException(
            $"Provedor de boleto '{codigo}' não está registrado. " +
            "Cadastre um adapter IBoletoProvider e inclua-o no catálogo.");
    }
}
