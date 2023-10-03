using QuoteEngineService.Caches;

namespace QuoteEngineService.Handlers;

public class OptionsPricingHandler : IOptionsPricingHandler
{
    private readonly ILogger<OptionsPricingHandler> _logger;
    private IConfigParameterSetCache? _configParameterSetCache;

    public OptionsPricingHandler(ILogger<OptionsPricingHandler> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(List<string> optionsList, IConfigParameterSetCache configParameterSetCache,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting options pricing handler");
        _configParameterSetCache = configParameterSetCache;
        await Parallel.ForEachAsync(optionsList, stoppingToken, ProcessOptionPrice);
    }

    private ValueTask ProcessOptionPrice(string optionSymbol, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pricing option: {OptionSymbol}", optionSymbol);
        var tokens = optionSymbol.Split('O');
        var underlier = tokens[0];

        while (stoppingToken.IsCancellationRequested)
        {
            // cps lookup
            var psUpdateMessage = _configParameterSetCache?.GetCurrentConfigSet(underlier);

            if(psUpdateMessage == null)
            {
                _logger.LogError("Could not find PSUpdateMessage for {OptionSymbol}", optionSymbol);
                continue;
            }

        }

        return ValueTask.CompletedTask;
    }

}
