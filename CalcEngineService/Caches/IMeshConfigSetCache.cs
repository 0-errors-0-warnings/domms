using CalcEngineService.Configs;
using CalcEngineService.DTO;

namespace CalcEngineService.Caches;

public interface IMeshConfigSetCache
{
    public UnderlierConfigSet? GetCurrentConfigSet(string underlier);

    public void UpdateConfigSet(MeshConfigSetConfiguration meshConfigSetConfiguration);
}
