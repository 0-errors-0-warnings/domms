using CalcEngineService.Configs;
using CalcEngineService.DTO;

namespace CalcEngineService.Caches;

public class UnderlierConfigSetCache
{
    private readonly UnderlierConfigSet[] _currentUnderlierConfigSet = new UnderlierConfigSet[2];
    private readonly object _syncObj = new();
    private int _currentIndex = 0;

    public UnderlierConfigSetCache()
    {
        //TODO: ensure cache is warmed up before use
        _currentUnderlierConfigSet[0] = new UnderlierConfigSet();
        _currentUnderlierConfigSet[1] = new UnderlierConfigSet();
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

    public UnderlierConfigSet CurrentUnderlierConfigSet
    {
        get
        {
            lock (_syncObj)
            {
                return _currentUnderlierConfigSet[_currentIndex];
            }
        }
    }

    public void UpdateParameterSet(MeshConfigSetConfiguration meshConfigSetConfiguration)
    {
        lock (_syncObj)
        {
            _currentIndex = _currentIndex == 0 ? 1 : 0;
            _currentUnderlierConfigSet[_currentIndex].Underlier = meshConfigSetConfiguration.Underlier;
            _currentUnderlierConfigSet[_currentIndex].SpotRangePct = meshConfigSetConfiguration.SpotRangePct;
            _currentUnderlierConfigSet[_currentIndex].SpotStepSizePct = meshConfigSetConfiguration.SpotStepSizePct;
            _currentUnderlierConfigSet[_currentIndex].VolRangePct = meshConfigSetConfiguration.VolRangePct;
            _currentUnderlierConfigSet[_currentIndex].VolStepSizePct = meshConfigSetConfiguration.VolStepSizePct;
            _currentUnderlierConfigSet[_currentIndex].RateRangePct = meshConfigSetConfiguration.RateRangePct;
            _currentUnderlierConfigSet[_currentIndex].RateStepSizePct = meshConfigSetConfiguration.RateStepSizePct;
            _currentUnderlierConfigSet[_currentIndex].DivRangePct = meshConfigSetConfiguration.DivRangePct;
            _currentUnderlierConfigSet[_currentIndex].DivStepSizePct = meshConfigSetConfiguration.DivStepSizePct;
            _currentUnderlierConfigSet[_currentIndex].TimeRangeMins= meshConfigSetConfiguration.TimeRangeMins;
            _currentUnderlierConfigSet[_currentIndex].TimeStepSizeMins = meshConfigSetConfiguration.TimeStepSizeMins;
        }
    }
}
