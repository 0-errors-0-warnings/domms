using MarketDataDistributionService.Messages;

namespace QuoteEngineService.Caches;

public interface IConfigParameterSetCache
{
    public ParameterSetUpdateMessage? GetCurrentConfigSet(string underlier);

    public void UpdateConfigSet(ParameterSetUpdateMessage parameterSetUpdateMessage);
}
