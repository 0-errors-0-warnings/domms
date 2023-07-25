using MarketDataDistributionService.Messages;

namespace CalcEngineService.Caches;

public interface IParameterSetUpdateCache
{
    public int CurrentIndex { get; }

    public ParameterSetUpdateMessage CurrentParameterSet { get; }

    public void UpdateParameterSet(ParameterSetUpdateMessage parameterSetUpdateMessage);
}
