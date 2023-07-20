namespace MarketDataService.Configs;

public class InstrumentConfiguration
{
    public string Ticker { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Spread { get; set; }
}
