namespace CalcEngineService.Handlers;

public interface IServiceHandler
{
    Task StartAsync(CancellationToken stoppingToken);
}
