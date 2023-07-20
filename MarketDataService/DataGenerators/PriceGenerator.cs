using MarketDataService.Configs;
using MarketDataService.Distributions;
using Microsoft.Extensions.Options;

namespace MarketDataService.DataGenerators;

public class PriceGenerator
{
    private readonly InstrumentConfiguration _instrumentConfiguration;
    private readonly IDistribution _distribution;

    public PriceGenerator(IDistribution distribution, IOptions<InstrumentConfiguration> instrumentConfigurationOption)
    {
        _distribution = distribution;
        _instrumentConfiguration = instrumentConfigurationOption.Value;
    }

    public (string, double, double, DateTime) GetPrice()
    {
        var midPx = _instrumentConfiguration.Max * _distribution.GetVal() + _instrumentConfiguration.Min;
        var halfSpread = _instrumentConfiguration.Spread / 2.0;
        var bidPx = Math.Round(midPx - halfSpread, 2, MidpointRounding.AwayFromZero);
        var askPx = Math.Round(midPx + halfSpread, 2, MidpointRounding.AwayFromZero);

        return (_instrumentConfiguration.Ticker, bidPx, askPx, DateTime.UtcNow);
    }
}
