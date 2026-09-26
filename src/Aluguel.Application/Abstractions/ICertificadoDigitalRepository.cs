using Aluguel.Domain.Fiscal;

namespace Aluguel.Application.Abstractions;

public interface ICertificadoDigitalRepository
{
    Task<CertificadoDigital?> ObterAtivoPorClienteAsync(Guid clienteId, CancellationToken ct = default);

    Task<IReadOnlyList<CertificadoDigital>> ListarAtivosPorClienteAsync(Guid clienteId, CancellationToken ct = default);

    void Adicionar(CertificadoDigital certificado);

    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}
