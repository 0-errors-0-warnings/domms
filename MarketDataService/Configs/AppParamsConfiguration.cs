namespace MarketDataService.Configs;

public class AppParamsConfiguration
{
    public string ZeroMqHost { get; set; }
    public int ZeroMqSendPort { get; set; }
    public string ZeroMqSendTopic { get; set; }
    public int NumOfThreads { get; set; }
}
