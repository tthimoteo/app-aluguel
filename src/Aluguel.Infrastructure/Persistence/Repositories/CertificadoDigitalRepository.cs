using Aluguel.Application.Abstractions;
using Aluguel.Domain.Fiscal;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

public sealed class CertificadoDigitalRepository(AppDbContext db) : ICertificadoDigitalRepository
{
    public Task<CertificadoDigital?> ObterAtivoPorClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        db.Certificados.FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Ativo, ct);

    public async Task<IReadOnlyList<CertificadoDigital>> ListarAtivosPorClienteAsync(
        Guid clienteId, CancellationToken ct = default) =>
        await db.Certificados
            .Where(c => c.ClienteId == clienteId && c.Ativo)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public void Adicionar(CertificadoDigital certificado) => db.Certificados.Add(certificado);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
