namespace QuoteEngineService.Handlers;

public interface IMddsServiceHandler
{
    Task StartAsync(CancellationToken stoppingToken);
}
