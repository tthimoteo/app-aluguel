using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aluguel.Infrastructure.Persistence;

/// <summary>Factory usada pelo `dotnet ef` em tempo de design (gera/aplica migrations sem subir a API).</summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ALUGUEL_DB")
            ?? "Host=127.0.0.1;Port=5432;Database=aluguel;Username=aluguel;Password=aluguel_dev";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
