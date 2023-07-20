using MarketDataService.Handlers;

namespace MarketDataService;

public class Worker : BackgroundService
{
    private readonly IServiceHandler _serviceHandler;

    public Worker(IServiceHandler serviceHandler)
    {
        _serviceHandler = serviceHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _serviceHandler.StartAsync(stoppingToken);
    }
}