using MarketDataService.Configs;
using Microsoft.Extensions.Options;

namespace MarketDataService.DataGenerators;

public class DivYieldGenerator
{
    private readonly OptionParamsConfiguration _optionParams;

    public DivYieldGenerator(IOptions<OptionParamsConfiguration> optionParamsConfigurationOption)
    {
        _optionParams = optionParamsConfigurationOption.Value;
    }

    public double GetDivYieldPct() => _optionParams.DividendYieldPct;
}
