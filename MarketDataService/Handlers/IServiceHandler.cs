namespace MarketDataService.Handlers;

public interface IServiceHandler
{
    Task StartAsync(CancellationToken stoppingToken);
}
