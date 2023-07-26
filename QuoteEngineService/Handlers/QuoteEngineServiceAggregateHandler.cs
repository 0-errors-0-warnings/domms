using Microsoft.Extensions.Options;
using NetMQ;
using QuoteEngineService.Configs;

namespace QuoteEngineService.Handlers;

public class QuoteEngineServiceAggregateHandler: IServiceHandler
{
    private readonly ILogger<QuoteEngineServiceAggregateHandler> _aggregateLogger;
    private readonly ILogger<QuoteEngineServiceCalcEngineHandler> _underlyingLogger;
    private readonly List<ZeroMqReceiveParamsConfiguration> _zeroMqReceiveParamsConfiguration;
    private readonly IMddsServiceHandler _mddsServiceHandler;

    public QuoteEngineServiceAggregateHandler(ILogger<QuoteEngineServiceAggregateHandler> aggregateLogger,
        ILogger<QuoteEngineServiceCalcEngineHandler> calcEngineLogger,
        IOptions<List<ZeroMqReceiveParamsConfiguration>> zeroMqReceiveParamsConfigurationOption,
        IMddsServiceHandler mddsServiceHandler)
    {
        _aggregateLogger = aggregateLogger;
        _underlyingLogger = calcEngineLogger;
        _zeroMqReceiveParamsConfiguration = zeroMqReceiveParamsConfigurationOption.Value;
        _mddsServiceHandler = mddsServiceHandler;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _aggregateLogger.LogInformation("Starting aggregate handler...");

        var mddsTask = Task.Run(() => _mddsServiceHandler.StartAsync(stoppingToken), stoppingToken);

        var subscribersList = new List<Task>
        {
            mddsTask
        };

        foreach (var subscriberCfg in _zeroMqReceiveParamsConfiguration)
        {
            subscribersList.Add(new QuoteEngineServiceCalcEngineHandler(_underlyingLogger, subscriberCfg).StartAsync(stoppingToken));
        }

        Task.WaitAll(subscribersList.ToArray(), stoppingToken);
        return Task.CompletedTask;
    }
}
