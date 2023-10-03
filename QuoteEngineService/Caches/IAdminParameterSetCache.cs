using QuoteEngineService.Configs;

namespace QuoteEngineService.Caches;

public interface IAdminParameterSetCache
{
    public AdminParamSetConfiguration? GetCurrentConfigSet(string underlier);

    public void UpdateConfigSet(AdminParamSetConfiguration adminParamSetConfiguration);
}
