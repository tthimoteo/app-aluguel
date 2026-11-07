using Aluguel.Application.Assinaturas.ProcessarCicloAssinaturas;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aluguel.Infrastructure.Assinaturas;

/// <summary>Dispara o processamento do ciclo de assinaturas a cada hora.</summary>
public sealed class AssinaturaCicloHostedService(
    IServiceScopeFactory scopes,
    ILogger<AssinaturaCicloHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguarda um pouco no boot para migrations/seed.
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var r = await sender.Send(new ProcessarCicloAssinaturasCommand(), stoppingToken);
                if (r.CancelamentosEfetivados + r.RenovacoesGeradas + r.DowngradesAplicados > 0)
                {
                    logger.LogInformation(
                        "Ciclo assinaturas: cancelados={Cancelados}, renovações={Renovacoes}, downgrades={Downgrades}",
                        r.CancelamentosEfetivados, r.RenovacoesGeradas, r.DowngradesAplicados);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao processar ciclo de assinaturas");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
