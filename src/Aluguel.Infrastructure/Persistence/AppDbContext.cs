using System.Reflection;
using Aluguel.Application.Abstractions;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Auditoria;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using Aluguel.Domain.Imoveis;
using Aluguel.Domain.Inquilinos;
using Aluguel.Domain.Tenancy;
using Aluguel.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>Exposto para uso nos filtros globais (parametrizado pelo EF a cada consulta).</summary>
    public Guid? CurrentTenantId => _currentTenant.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plano> Planos => Set<Plano>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<PagamentoPlano> PagamentosPlano => Set<PagamentoPlano>();
    public DbSet<AuditoriaAssinatura> AuditoriasAssinatura => Set<AuditoriaAssinatura>();
    public DbSet<Imovel> Imoveis => Set<Imovel>();
    public DbSet<Inquilino> Inquilinos => Set<Inquilino>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<NotaFiscalServico> NotasFiscais => Set<NotaFiscalServico>();
    public DbSet<DocumentoFiscal> DocumentosFiscais => Set<DocumentoFiscal>();
    public DbSet<CertificadoDigital> Certificados => Set<CertificadoDigital>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<Despesa> Despesas => Set<Despesa>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Identity.RefreshToken> RefreshTokens => Set<Identity.RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("app");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigurarFiltrosGlobais(builder);
    }

    private void ConfigurarFiltrosGlobais(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (entityType.IsOwned()) continue;

            var tenantOwned = typeof(ITenantOwned).IsAssignableFrom(clr);
            var softDeletable = typeof(ISoftDeletable).IsAssignableFrom(clr);

            MethodInfo? method = (tenantOwned, softDeletable) switch
            {
                (true, true) => TenantSoftDeleteMethod.MakeGenericMethod(clr),
                (true, false) => TenantMethod.MakeGenericMethod(clr),
                (false, true) => SoftDeleteMethod.MakeGenericMethod(clr),
                _ => null,
            };
            method?.Invoke(this, [builder]);
        }
    }

    private static readonly MethodInfo TenantMethod =
        typeof(AppDbContext).GetMethod(nameof(FiltroTenant), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo SoftDeleteMethod =
        typeof(AppDbContext).GetMethod(nameof(FiltroSoftDelete), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo TenantSoftDeleteMethod =
        typeof(AppDbContext).GetMethod(nameof(FiltroTenantSoftDelete), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private void FiltroTenant<T>(ModelBuilder b) where T : class, ITenantOwned =>
        b.Entity<T>().HasQueryFilter(e => CurrentTenantId == null || e.TenantId == CurrentTenantId);

    private void FiltroSoftDelete<T>(ModelBuilder b) where T : class, ISoftDeletable =>
        b.Entity<T>().HasQueryFilter(e => e.DeletedAt == null);

    private void FiltroTenantSoftDelete<T>(ModelBuilder b) where T : class, ITenantOwned, ISoftDeletable =>
        b.Entity<T>().HasQueryFilter(e => (CurrentTenantId == null || e.TenantId == CurrentTenantId) && e.DeletedAt == null);
}
