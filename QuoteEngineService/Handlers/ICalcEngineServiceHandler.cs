namespace QuoteEngineService.Handlers;

public interface ICalcEngineServiceHandler
{
    Task StartAsync(CancellationToken stoppingToken);
}
