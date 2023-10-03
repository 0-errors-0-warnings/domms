namespace QuoteEngineService.Caches;

public class ParameterCache<T>: IParameterCache<T> where T : class, new()
{
    private readonly T[] _currentParameterSet = new T[2];
    private readonly object _syncObj = new();
    private int _currentIndex = 0;

    public ParameterCache()
    {
        //TODO: ensure cache is warmed up before use
        _currentParameterSet[0] = new T();
        _currentParameterSet[1] = new T();
    }

    public int CurrentIndex
    {
        get
        {
            lock (_syncObj)
            {
                return _currentIndex;
            }
        }
    }

    public T GetCurrent
    {
        get
        {
            lock (_syncObj)
            {
                return _currentParameterSet[_currentIndex];
            }
        }
    }

    public void Update(T parameterSetUpdateMessage)
    {
        lock (_syncObj)
        {
            _currentIndex = _currentIndex == 0 ? 1 : 0;
            _currentParameterSet[_currentIndex] = parameterSetUpdateMessage;
        }
    }
}
