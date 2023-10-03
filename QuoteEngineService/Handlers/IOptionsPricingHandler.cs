using MarketDataDistributionService.Messages;
using QuoteEngineService.Caches;

namespace QuoteEngineService.Handlers;

public interface IOptionsPricingHandler
{
    Task StartAsync(List<string> optionsList, IConfigParameterSetCache configParameterSetCache, CancellationToken stoppingToken);
}
