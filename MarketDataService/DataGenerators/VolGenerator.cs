using MarketDataService.Configs;
using MarketDataService.Distributions;
using Microsoft.Extensions.Options;

namespace MarketDataService.DataGenerators;

public class VolGenerator
{
    private readonly VolatilityParamsConfiguration _volatilityParamsConfiguration;
    private readonly IDistribution _distribution;

    public VolGenerator(IDistribution distribution, IOptions<VolatilityParamsConfiguration> volatilityParamsConfigurationOption)
    {
        _distribution = distribution;
        _volatilityParamsConfiguration = volatilityParamsConfigurationOption.Value;
    }

    public double GetVolPct() => 
        Math.Round(_volatilityParamsConfiguration.MaxPct * _distribution.GetVal() + _volatilityParamsConfiguration.MinPct, 4, MidpointRounding.AwayFromZero);
}
