namespace QuoteEngineService.Handlers;

public interface IUnderlyingServiceHandler
{
    Task StartAsync(CancellationToken stoppingToken);
}
