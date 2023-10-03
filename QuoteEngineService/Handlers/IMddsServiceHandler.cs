using MarketDataDistributionService.Messages;
using QuoteEngineService.Caches;

namespace QuoteEngineService.Handlers;

public interface IMddsServiceHandler
{
    Task StartAsync(IConfigParameterSetCache configParameterSetCache, CancellationToken stoppingToken);
}
