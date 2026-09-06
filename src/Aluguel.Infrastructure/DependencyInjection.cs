using Aluguel.Application.Abstractions;
using Aluguel.Infrastructure.Identity;
using Aluguel.Infrastructure.Persistence;
using Aluguel.Infrastructure.Persistence.Repositories;
using Aluguel.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aluguel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

        // Resolução de tenant: default nulo (a API registra a versão baseada no JWT ao habilitar auth).
        services.AddScoped<ICurrentTenant, NullCurrentTenant>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        services.AddIdentityCore<AppUser>(o =>
            {
                o.Password.RequiredLength = 10;
                o.User.RequireUniqueEmail = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        services.AddScoped<IPlanoRepository, PlanoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IImovelRepository, ImovelRepository>();
        services.AddScoped<IInquilinoRepository, InquilinoRepository>();
        services.AddScoped<IContratoRepository, ContratoRepository>();

        return services;
    }
}
