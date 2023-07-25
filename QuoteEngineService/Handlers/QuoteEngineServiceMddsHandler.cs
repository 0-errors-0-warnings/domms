using CalcEngineService.Messages;
using MarketDataDistributionService.Messages;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using QuoteEngineService.Configs;
using System.Threading.Channels;

namespace QuoteEngineService.Handlers;

public class QuoteEngineServiceMddsHandler : IMddsServiceHandler
{
    private readonly ILogger<QuoteEngineServiceMddsHandler> _logger;
    private readonly ZeroMqReceiveParamsConfiguration _zeroMqReceiveParamsConfiguration;
    private Channel<ParameterSetUpdateMessage> _inboundChannel;
    private List<Task> _processorTaskList;

    public QuoteEngineServiceMddsHandler(ILogger<QuoteEngineServiceMddsHandler> logger, 
        IOptions<ZeroMqReceiveParamsConfiguration> zeroMqReceiveParamsConfigurationOption)
    {
        _logger = logger;
        _zeroMqReceiveParamsConfiguration = zeroMqReceiveParamsConfigurationOption.Value;
        _inboundChannel = Channel.CreateBounded<ParameterSetUpdateMessage>(_zeroMqReceiveParamsConfiguration.ReceiveMessageCapacity);
        _processorTaskList = new List<Task>();
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        // start processor tasks
        for (int i = 0; i < _zeroMqReceiveParamsConfiguration.NumOfThreads; i++)
        {
            _processorTaskList.Add(Task.Run(() => ProcessMddsMessage(stoppingToken), stoppingToken));
        } 

        using var runtime = new NetMQRuntime();
        runtime.Run(stoppingToken,
            SubscriberAsync(stoppingToken,
            _zeroMqReceiveParamsConfiguration.ZeroMqReceiveHost,
            _zeroMqReceiveParamsConfiguration.ZeroMqReceivePort,
            _zeroMqReceiveParamsConfiguration.ZeroMqReceiveTopic,
            _zeroMqReceiveParamsConfiguration.ReceiveMessageCapacity,
            WriteToInbound));

        await Task.WhenAll(_processorTaskList);
    }

    #region zeromq calls - abstract this out later

    private async Task SubscriberAsync(CancellationToken stoppingToken, string host, int port, string topic, int maxCapacity, Action<byte[]> saveToChannel)
    {
        _logger.LogInformation("Subscriber binding on {Host}:{Port}, Topic: {Topic} ", host, port, topic);

        using var subSocket = new SubscriberSocket();
        subSocket.Options.ReceiveHighWatermark = maxCapacity;
        subSocket.Connect($"tcp://{host}:{port}");
        subSocket.Subscribe(topic);

        var i = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var (_, anotherFrame) = await subSocket.ReceiveFrameStringAsync(stoppingToken);
            if (!anotherFrame)
            {
                continue;
            }
            var (bytes, _) = await subSocket.ReceiveFrameBytesAsync(stoppingToken);
            saveToChannel(bytes);
            //_logger.LogInformation("MessageCount: {i}", ++i);
        }
    }

    #endregion

    private async void WriteToInbound(byte[] bytes)
    {
        var msg = ParameterSetUpdateMessage.Parser.ParseFrom(bytes);
        await _inboundChannel.Writer.WriteAsync(msg);
    }

    int count = 0;
    private async void ProcessMddsMessage(CancellationToken stoppingToken)
    {
        // pick the latest message
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetUpdateMessage = await _inboundChannel.Reader.ReadAsync(stoppingToken);

            count++;

            if (count % 50000 == 0)
            {
                _logger.LogInformation("parameterSetUpdateMessage: {Underlier}, PriceTime: {PriceTime}", parameterSetUpdateMessage.Underlier, parameterSetUpdateMessage.PriceTime);
            }
            //TODO: update PS via double buffering
            //TODO: publish
        }
    }


}
