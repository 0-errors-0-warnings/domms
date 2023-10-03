using MarketDataDistributionService.Messages;
using System.Collections.Concurrent;

namespace QuoteEngineService.Caches;

public class ConfigParameterSetCache : IConfigParameterSetCache
{
    private readonly ConcurrentDictionary<string, IParameterCache<ParameterSetUpdateMessage>> _parameterSetUpdateMessageCache = new();


    public ParameterSetUpdateMessage? GetCurrentConfigSet(string underlier)
    {
        if (_parameterSetUpdateMessageCache.TryGetValue(underlier, out var adminParamSetConfigurationCache))
        {
            return adminParamSetConfigurationCache.GetCurrent;
        }

        return null;
    }

    public void UpdateConfigSet(ParameterSetUpdateMessage parameterSetUpdateMessage)
    {
        var key = parameterSetUpdateMessage.Underlier;
        if (!_parameterSetUpdateMessageCache.TryGetValue(key, out var adminParamSetConfigurationCache))
        {
            adminParamSetConfigurationCache = new ParameterCache<ParameterSetUpdateMessage>();
            _parameterSetUpdateMessageCache[key] = adminParamSetConfigurationCache;
        }
        adminParamSetConfigurationCache.Update(parameterSetUpdateMessage);
    }
}
