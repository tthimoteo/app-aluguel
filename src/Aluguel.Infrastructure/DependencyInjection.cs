using Aluguel.Application.Abstractions;
using Aluguel.Domain.Fiscal;
using Aluguel.Infrastructure.Identity;
using Aluguel.Infrastructure.Integrations.NfseNacional;
using Aluguel.Infrastructure.Persistence;
using Aluguel.Infrastructure.Persistence.Repositories;
using Aluguel.Infrastructure.Security;
using Aluguel.Infrastructure.Storage;
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
        services.AddScoped<ICertificadoDigitalRepository, CertificadoDigitalRepository>();
        services.AddScoped<ICobrancaRepository, CobrancaRepository>();

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<ISecretProtector, AesSecretProtector>();

        services.Configure<NfseOptions>(opts =>
        {
            var s = configuration.GetSection(NfseOptions.SectionName);
            if (s["Ambiente"] is { Length: > 0 } amb) opts.Ambiente = amb;
            if (s["VerAplic"] is { Length: > 0 } ver) opts.VerAplic = ver;
            if (s["SeriePadrao"] is { Length: > 0 } ser) opts.SeriePadrao = ser;
            if (s["CodigoMunicipioIbgePadrao"] is { Length: > 0 } mun) opts.CodigoMunicipioIbgePadrao = mun;
            if (s["CodigoTributacaoNacionalPadrao"] is { Length: > 0 } trib) opts.CodigoTributacaoNacionalPadrao = trib;
            if (decimal.TryParse(s["PercentualTotTribSn"], System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var p))
                opts.PercentualTotTribSn = p;
            if (s["FinNfse"] is { Length: > 0 } fin) opts.FinNfse = fin;
            if (s["CodigoIndicadorOperacaoIbsCbs"] is { } ind)
                opts.CodigoIndicadorOperacaoIbsCbs = string.IsNullOrWhiteSpace(ind) ? null : ind;
            if (s["CstIbsCbs"] is { Length: > 0 } cst) opts.CstIbsCbs = cst;
            if (s["ClassificacaoTributariaIbsCbs"] is { Length: > 0 } cls) opts.ClassificacaoTributariaIbsCbs = cls;
            if (bool.TryParse(s["IncluirIbsCbs"], out var ibs)) opts.IncluirIbsCbs = ibs;
            if (s["CodigoNbsPadrao"] is { } nbsCfg)
                opts.CodigoNbsPadrao = string.IsNullOrWhiteSpace(nbsCfg)
                    ? NfseCodigosPadrao.CodigoNbsResidencial
                    : nbsCfg;
            if (bool.TryParse(s["EnviarImPrestador"], out var im)) opts.EnviarImPrestador = im;
        });
        var nfseAmbiente = configuration[$"{NfseOptions.SectionName}:Ambiente"] ?? "Mock";
        if (string.Equals(nfseAmbiente, "Mock", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<INfseNacionalProvider, MockNfseNacionalProvider>();
        else
            services.AddScoped<INfseNacionalProvider, SefinNfseNacionalProvider>();

        return services;
    }
}
