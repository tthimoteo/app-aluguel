using System.Text;
using Aluguel.Api.Auth;
using Aluguel.Api.Endpoints;
using Aluguel.Api.Setup;
using Aluguel.Application;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Autorizacao;
using Aluguel.Application.Planos.ObterPlanos;
using Aluguel.Infrastructure;
using Aluguel.Infrastructure.Identity;
using Aluguel.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Resolução de tenant a partir do JWT (sobrescreve o provider nulo registrado na Infraestrutura).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenant, JwtCurrentTenant>();

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey é obrigatória e deve ter pelo menos 32 caracteres.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            RoleClaimType = "role",
            NameClaimType = "name",
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Politicas.GerenciaUsuarios, p => p.RequireRole(Perfis.Administrador, Perfis.Gestor));
    o.AddPolicy(Politicas.EmitirNfse, p => p.RequireRole(Perfis.Administrador, Perfis.Gestor, Perfis.Analista));
    o.AddPolicy(Politicas.CancelarNfse, p => p.RequireRole(Perfis.Administrador, Perfis.Gestor, Perfis.Analista));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "APP Aluguel API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe apenas o token JWT (o prefixo 'Bearer' é adicionado automaticamente).",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

if (builder.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Roles sempre; usuários demo apenas em Development.
await IdentitySeeder.SeedAsync(app.Services, seedDemoUsers: app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }))
    .WithName("Health");

app.MapGet("/api/planos", async (ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new ObterPlanosQuery(), ct)))
    .WithName("ObterPlanos");

app.MapAuthEndpoints();

// Endpoints de diagnóstico para demonstrar a autorização por role/política.
app.MapGet("/api/admin/ping", () => Results.Ok(new { escopo = "Administrador", ok = true }))
    .RequireAuthorization(p => p.RequireRole(Perfis.Administrador))
    .WithTags("Diagnóstico");

app.MapGet("/api/gestao/ping", () => Results.Ok(new { escopo = "GerenciaUsuarios", ok = true }))
    .RequireAuthorization(Politicas.GerenciaUsuarios)
    .WithTags("Diagnóstico");

app.MapGet("/api/nfse/ping", () => Results.Ok(new { escopo = "EmitirNfse", ok = true }))
    .RequireAuthorization(Politicas.EmitirNfse)
    .WithTags("Diagnóstico");

app.Run();

public partial class Program { }
