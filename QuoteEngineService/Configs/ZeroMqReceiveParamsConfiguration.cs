namespace QuoteEngineService.Configs;

public class ZeroMqReceiveParamsConfiguration
{
    public string SubscriptionTicker { get; set; }
    public string SubscriptionType { get; set; }
    public string ZeroMqReceiveHost { get; set; }
    public int ZeroMqReceivePort { get; set; }
    public string ZeroMqReceiveTopic { get; set; }
    public int ReceiveMessageCapacity { get; set; }
    public int NumOfThreads { get; set; }
}
