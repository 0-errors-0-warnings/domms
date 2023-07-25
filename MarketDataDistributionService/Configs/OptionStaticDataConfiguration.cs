namespace MarketDataDistributionService.Configs;

public class OptionStaticDataConfiguration
{
    public string Underlier { get; set; }
    public double StrikePrice { get; set; }
    public int OptionExpiryDateInDays { get; set; }
}
