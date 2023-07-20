namespace MarketDataDistributionService.Handlers;

public interface IServiceHandler
{
    Task StartAsync(CancellationToken stoppingToken);
}
