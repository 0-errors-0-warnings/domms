using MarketDataDistributionService.Messages;

namespace CalcEngineService.Caches;

public class ParameterSetUpdateCache : IParameterSetUpdateCache
{
    private readonly ParameterSetUpdateMessage[] _currentParameterSet = new ParameterSetUpdateMessage[2];
    private readonly object _syncObj = new();
    private int _currentIndex = 0;

    public ParameterSetUpdateCache()
    {
        //TODO: ensure cache is warmed up before use
        _currentParameterSet[0] = new ParameterSetUpdateMessage();
        _currentParameterSet[1] = new ParameterSetUpdateMessage();
    }

    public int CurrentIndex
    {
        get
        {
            lock(_syncObj)
            {
                return _currentIndex;
            }
        }
    }

    public ParameterSetUpdateMessage CurrentParameterSet 
    {
        get
        {
            lock (_syncObj)
            {
                return _currentParameterSet[_currentIndex];
            }
        }
    }

    public void UpdateParameterSet(ParameterSetUpdateMessage parameterSetUpdateMessage)
    {
        lock(_syncObj)
        {
            _currentIndex = _currentIndex == 0 ? 1 : 0;
            _currentParameterSet[_currentIndex] = parameterSetUpdateMessage;
        }
    }
}
