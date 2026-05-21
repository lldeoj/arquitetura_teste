using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceConsolidado.Interface;

namespace ServiceConsolidado;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started. Creating scope for processing services...");

        // Create a scope so we can resolve scoped services (like DbContext and IConsolidadoService)
        using var scope = _serviceProvider.CreateScope();
        var consolidadoProcessService = scope.ServiceProvider.GetRequiredService<IConsolidadoProcessService>();

        // Start listeners (non-blocking). The resolved scoped service and scope must live while listeners run.
        consolidadoProcessService.ListenerOnRabbitMqQueue(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Worker cancellation requested.");
        }

        _logger.LogInformation("Worker stopping, disposing scope.");
    }
}

