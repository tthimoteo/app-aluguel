using Aluguel.Application;
using Aluguel.Application.Planos.ObterPlanos;
using Aluguel.Infrastructure;
using Aluguel.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplica migrations no startup (Sprint 0: conveniência de dev/staging).
if (builder.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }))
    .WithName("Health");

app.MapGet("/api/planos", async (ISender sender, CancellationToken ct) =>
        Results.Ok(await sender.Send(new ObterPlanosQuery(), ct)))
    .WithName("ObterPlanos");

app.Run();

public partial class Program { }
