using Aluguel.Application.Abstractions;
using Aluguel.Domain.Fiscal;
using Aluguel.Infrastructure.Identity;
using Aluguel.Infrastructure.Integrations.BoletoBancario;
using Aluguel.Infrastructure.Integrations.NfseNacional;
using Aluguel.Infrastructure.Assinaturas;
using Aluguel.Infrastructure.Integrations.PixInter;
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
        services.AddScoped<IAssinaturaRepository, AssinaturaRepository>();
        services.AddScoped<IImovelRepository, ImovelRepository>();
        services.AddScoped<IInquilinoRepository, InquilinoRepository>();
        services.AddScoped<IContratoRepository, ContratoRepository>();
        services.AddScoped<ICertificadoDigitalRepository, CertificadoDigitalRepository>();
        services.AddScoped<ICobrancaRepository, CobrancaRepository>();
        services.AddScoped<IIntegracaoBoletoRepository, IntegracaoBoletoRepository>();
        services.AddScoped<IRelatorioRepository, RelatorioRepository>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<ISecretProtector, AesSecretProtector>();
        services.AddSingleton<INfsePdfGenerator, NfseDanfsePdfGenerator>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<AssinaturaCicloHostedService>();
        services.AddHttpClient();

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

        services.Configure<BoletoOptions>(opts =>
        {
            var s = configuration.GetSection(BoletoOptions.SectionName);
            if (s["Ambiente"] is { Length: > 0 } amb) opts.Ambiente = amb;
            var urls = s.GetSection("BaseUrls");
            foreach (var child in urls.GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
                    opts.BaseUrls[child.Key] = child.Value!;
            }
        });

        services.AddScoped<IBoletoProvider, ItauBoletoProvider>();
        services.AddScoped<IBoletoProvider, BradescoBoletoProvider>();
        services.AddScoped<IBoletoProvider, BancoDoBrasilBoletoProvider>();
        services.AddScoped<IBoletoProvider, SantanderBoletoProvider>();
        services.AddScoped<IBoletoProvider, CaixaBoletoProvider>();
        services.AddScoped<IBoletoProvider, SicrediBoletoProvider>();
        services.AddScoped<IBoletoProvider, MercadoPagoBoletoProvider>();
        services.AddScoped<IBoletoProvider, InterBoletoProvider>();
        services.AddScoped<IBoletoProvider, C6BoletoProvider>();
        services.AddScoped<IBoletoProvider, NubankBoletoProvider>();
        services.AddScoped<IBoletoProviderFactory, BoletoProviderFactory>();

        services.Configure<InterPixOptions>(opts =>
        {
            var s = configuration.GetSection(InterPixOptions.SectionName);
            if (s["Ambiente"] is { Length: > 0 } amb) opts.Ambiente = amb;
            if (s["UrlBase"] is { Length: > 0 } url) opts.UrlBase = url;
            if (s["ClientId"] is { Length: > 0 } cid) opts.ClientId = cid;
            if (s["ClientSecret"] is { Length: > 0 } sec) opts.ClientSecret = sec;
            if (s["ContaCorrente"] is { Length: > 0 } cc) opts.ContaCorrente = cc;
            if (s["ChavePix"] is { Length: > 0 } chave) opts.ChavePix = chave;
            if (s["CertificadoPath"] is { Length: > 0 } path) opts.CertificadoPath = path;
            if (s["CertificadoSenha"] is { Length: > 0 } senha) opts.CertificadoSenha = senha;
            if (s["BeneficiarioNome"] is { Length: > 0 } nome) opts.BeneficiarioNome = nome;
            if (s["BeneficiarioCnpj"] is { Length: > 0 } cnpj) opts.BeneficiarioCnpj = cnpj;
            if (s["BeneficiarioBanco"] is { Length: > 0 } banco) opts.BeneficiarioBanco = banco;
            if (s["BeneficiarioContaCorrente"] is { Length: > 0 } bcc) opts.BeneficiarioContaCorrente = bcc;
            if (int.TryParse(s["ExpiracaoSegundosPadrao"], out var exp)) opts.ExpiracaoSegundosPadrao = exp;
        });
        var interPixOpts = new InterPixOptions();
        configuration.GetSection(InterPixOptions.SectionName).Bind(interPixOpts);
        services.AddInterPixHttpClient(interPixOpts);
        var interPixAmbiente = configuration[$"{InterPixOptions.SectionName}:Ambiente"] ?? "Mock";
        if (string.Equals(interPixAmbiente, "Mock", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IInterPixProvider, MockInterPixProvider>();
        else
            services.AddScoped<IInterPixProvider, InterPixProvider>();

        return services;
    }
}
