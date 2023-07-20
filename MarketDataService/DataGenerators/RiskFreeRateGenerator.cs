using MarketDataService.Configs;
using Microsoft.Extensions.Options;

namespace MarketDataService.DataGenerators;

public class RiskFreeRateGenerator
{
    private readonly OptionParamsConfiguration _optionParams;

    public RiskFreeRateGenerator(IOptions<OptionParamsConfiguration> optionParamsConfigurationOption)
    {
        _optionParams = optionParamsConfigurationOption.Value;
    }

    public double GetRiskFreeRatePct() => _optionParams.RiskFreeRatePct;
}
