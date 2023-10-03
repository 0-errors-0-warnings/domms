using QuoteEngineService.Configs;
using System.Collections.Concurrent;

namespace QuoteEngineService.Caches;

public class AdminParameterSetCache : IAdminParameterSetCache
{
    private readonly ConcurrentDictionary<string, IParameterCache<AdminParamSetConfiguration>> _adminParamSetConfigurationCache = new();


    public AdminParamSetConfiguration? GetCurrentConfigSet(string underlier)
    {
        if (_adminParamSetConfigurationCache.TryGetValue(underlier, out var adminParamSetConfigurationCache))
        {
            return adminParamSetConfigurationCache.GetCurrent;
        }

        return null;
    }

    public void UpdateConfigSet(AdminParamSetConfiguration adminParamSetConfiguration)
    {
        var key = adminParamSetConfiguration.OptSymbol;
        if (!_adminParamSetConfigurationCache.TryGetValue(key, out var adminParamSetConfigurationCache))
        {
            adminParamSetConfigurationCache = new ParameterCache<AdminParamSetConfiguration>();
            _adminParamSetConfigurationCache[key] = adminParamSetConfigurationCache;
        }
        adminParamSetConfigurationCache.Update(adminParamSetConfiguration);
    }
}
