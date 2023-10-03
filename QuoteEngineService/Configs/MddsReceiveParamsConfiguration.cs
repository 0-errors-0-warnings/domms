namespace QuoteEngineService.Configs;

public class MddsReceiveParamsConfiguration
{
    public string ZeroMqReceiveHost { get; set; }
    public int ZeroMqReceivePort { get; set; }
    public string ZeroMqReceiveTopic { get; set; }
    public int ReceiveMessageCapacity { get; set; }
}
