using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlantMonitor.Application.Common.Interfaces;

namespace PlantMonitor.Infrastructure.Services;

public class CommandCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandCleanupService> _logger;

    public CommandCleanupService(IServiceProvider serviceProvider, ILogger<CommandCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var commandsService = scope.ServiceProvider.GetRequiredService<IDeviceCommandsService>();
                
                await commandsService.CleanupExpiredCommandsAsync();
                
                // Run cleanup every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during command cleanup");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
