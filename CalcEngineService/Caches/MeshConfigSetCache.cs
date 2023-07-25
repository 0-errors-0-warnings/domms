using CalcEngineService.Configs;
using CalcEngineService.DTO;
using System.Collections.Concurrent;

namespace CalcEngineService.Caches;

public class MeshConfigSetCache : IMeshConfigSetCache
{
    private readonly ConcurrentDictionary<string, UnderlierConfigSetCache> _meshConfigSetCache = new();

    public UnderlierConfigSet? GetCurrentConfigSet(string underlier)
    {
        if (_meshConfigSetCache.TryGetValue(underlier, out var underlierConfigSetCache))
        {
            return underlierConfigSetCache.CurrentUnderlierConfigSet;
        }

        return null;
    }

    public void UpdateConfigSet(MeshConfigSetConfiguration meshConfigSetConfiguration)
    {
        if (!_meshConfigSetCache.TryGetValue(meshConfigSetConfiguration.Underlier, out var underlierConfigSetCache))
        {
            underlierConfigSetCache = new UnderlierConfigSetCache();
            _meshConfigSetCache[meshConfigSetConfiguration.Underlier] = underlierConfigSetCache;
        }
        underlierConfigSetCache.UpdateParameterSet(meshConfigSetConfiguration);
    }
}
