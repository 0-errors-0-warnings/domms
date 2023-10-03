using CalcEngineService.Messages;
using Microsoft.Extensions.Options;
using QuoteEngineService.Caches;
using QuoteEngineService.Configs;

namespace QuoteEngineService.Handlers;

public class QuoteEngineServiceAggregateHandler: IServiceHandler
{
    private readonly ILogger<QuoteEngineServiceAggregateHandler> _aggregateLogger;
    private readonly ILogger<QuoteEngineServiceCalcEngineHandler> _underlyingLogger;

    private readonly IOptionsPricingHandler _optionsPricingHandler;
    private readonly IMddsServiceHandler _mddsServiceHandler;
    private readonly IConfigParameterSetCache _configParameterSetCache;
    private readonly IParameterCache<ParameterSetMeshUpdateMessage> _parameterSetMeshUpdateCache;
    private readonly IAdminParameterSetCache _adminParameterSetCache;

    private readonly List<CalcEngineReceiveParamsConfiguration> _calcEngineReceiveParamsConfiguration;
    private readonly List<AdminParamSetConfiguration> _adminParamSetConfiguration;
    private readonly List<string> _optionsList = new();


    public QuoteEngineServiceAggregateHandler(ILogger<QuoteEngineServiceAggregateHandler> aggregateLogger,
        ILogger<QuoteEngineServiceCalcEngineHandler> calcEngineLogger,
        IOptions<List<CalcEngineReceiveParamsConfiguration>> calcEngineReceiveParamsConfigurationOption,
        IOptionsPricingHandler optionsPricingHandler,
        IMddsServiceHandler mddsServiceHandler,
        IConfigParameterSetCache configParameterSetCache,
        IParameterCache<ParameterSetMeshUpdateMessage> parameterSetMeshUpdateCache,
        IAdminParameterSetCache adminParameterSetCache,
        IOptions<List<AdminParamSetConfiguration>> adminParamSetConfigurationOption)
    {
        _aggregateLogger = aggregateLogger;
        _underlyingLogger = calcEngineLogger;
        _calcEngineReceiveParamsConfiguration = calcEngineReceiveParamsConfigurationOption.Value;
        _optionsPricingHandler = optionsPricingHandler;
        _mddsServiceHandler = mddsServiceHandler;
        _configParameterSetCache = configParameterSetCache;
        _parameterSetMeshUpdateCache = parameterSetMeshUpdateCache;
        _adminParameterSetCache = adminParameterSetCache;
        _adminParamSetConfiguration = adminParamSetConfigurationOption.Value;
        BuildInitialAdminParameterSetAndRefDataLookup();
    }


    public Task StartAsync(CancellationToken stoppingToken)
    {
        _aggregateLogger.LogInformation("Starting aggregate handler...");

        var mddsTask = Task.Run(() => _mddsServiceHandler.StartAsync(_configParameterSetCache, stoppingToken), stoppingToken);
        var optionspricingTask = Task.Run(() => 
        _optionsPricingHandler.StartAsync(_optionsList, _configParameterSetCache, stoppingToken), stoppingToken);

        var subscribersList = new List<Task>
        {
            mddsTask,
            optionspricingTask
        };

        foreach (var subscriberCfg in _calcEngineReceiveParamsConfiguration)
        {
            subscribersList.Add(new QuoteEngineServiceCalcEngineHandler(_underlyingLogger, subscriberCfg, _parameterSetMeshUpdateCache)
                .StartAsync(stoppingToken));
        }

        Task.WaitAll(subscribersList.ToArray(), stoppingToken);
        return Task.CompletedTask;
    }

    private void BuildInitialAdminParameterSetAndRefDataLookup()
    {
        foreach (var adminParamSet in _adminParamSetConfiguration)
        {
            _optionsList.Add(adminParamSet.OptSymbol);
            _adminParameterSetCache.UpdateConfigSet(adminParamSet);
        }
    }
}
