namespace CalcEngineService.Configs;

public class AppParamsConfiguration
{
    public string SubscriptionTicker { get; set; }
    public string ZeroMqReceiveHost { get; set; }
    public int ZeroMqReceivePort { get; set; }
    public string ZeroMqReceiveTopic { get; set; }
    public int ReceiveMessageCapacity { get; set; }
    public string ZeroMqSendHost { get; set; }
    public int ZeroMqSendPort { get; set; }
    public string ZeroMqSendTopic { get; set; }
    public int SendMessageCapacity { get; set; }
}
