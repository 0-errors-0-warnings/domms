namespace QuoteEngineService.Configs;

public class CalcEngineReceiveParamsConfiguration
{
    public string ZeroMqReceiveHost { get; set; }
    public int ZeroMqReceivePort { get; set; }
    public string ZeroMqReceiveTopic { get; set; }
    public int ReceiveMessageCapacity { get; set; }
}
