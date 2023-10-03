namespace QuoteEngineService.Caches;

public interface IParameterCache<T>
{
    public int CurrentIndex { get; }

    public T GetCurrent { get; }

    public void Update(T param);
}
