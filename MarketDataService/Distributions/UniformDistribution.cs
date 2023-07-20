namespace MarketDataService.Distributions;

public class UniformDistribution : IDistribution
{
    private readonly Random _rand;

    public UniformDistribution()
    {
        _rand = new Random(50);
    }

    public double GetVal() => _rand.NextDouble();
}
