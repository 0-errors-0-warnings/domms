namespace QuoteEngineService.Handlers;

public interface IServiceHandler
{
    Task StartAsync(CancellationToken stoppingToken);
}
