using Microsoft.Extensions.Options;
using MarketDataDistributionService.Messages;
using CalcEngineService.Messages;
using NetMQ;
using NetMQ.Sockets;
using System.Threading.Channels;
using CalcEngineService.Configs;
using Google.Protobuf;
using CalcEngineService.Extensions;

namespace CalcEngineService.Handlers;

public class CalcEngineServiceHandler : IServiceHandler
{
    private readonly ILogger<CalcEngineServiceHandler> _logger;
    private readonly AppParamsConfiguration _appParamsConfiguration;
    private Channel<ParameterSetUpdateMessage> _inboundChannel;
    private Channel<ParameterSetMeshUpdateMessage> _outboundChannel;
    private List<Task> _processorTaskList;

    public CalcEngineServiceHandler(ILogger<CalcEngineServiceHandler> logger, IOptions<AppParamsConfiguration> appParamsConfigurationOption)
    {
        _logger = logger;
        _appParamsConfiguration = appParamsConfigurationOption.Value;
        _inboundChannel = Channel.CreateBounded<ParameterSetUpdateMessage>(_appParamsConfiguration.ReceiveMessageCapacity);
        _outboundChannel = Channel.CreateBounded<ParameterSetMeshUpdateMessage>(_appParamsConfiguration.SendMessageCapacity);
        _processorTaskList = new List<Task>();
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        // start processor tasks
        for (int i = 0; i < _appParamsConfiguration.NumOfThreads; i++)
        {
            _processorTaskList.Add(Task.Run(() => ProcessMarketDataMessage(stoppingToken), stoppingToken));
        }

        using var runtime = new NetMQRuntime();
        //runtime.Run(stoppingToken, SubscriberAsync(stoppingToken, _inboundChannel.Writer), PublisherAsync(stoppingToken, _inboundChannel.Reader));
        runtime.Run(stoppingToken,
            SubscriberAsync(stoppingToken,
            _appParamsConfiguration.ZeroMqReceiveHost,
            _appParamsConfiguration.ZeroMqReceivePort,
            _appParamsConfiguration.ZeroMqReceiveTopic,
            _appParamsConfiguration.ReceiveMessageCapacity,
            WriteToInbound),
            PublisherAsync(stoppingToken,
            _appParamsConfiguration.ZeroMqSendHost,
            _appParamsConfiguration.ZeroMqSendPort,
            _appParamsConfiguration.ZeroMqSendTopic,
            _appParamsConfiguration.SendMessageCapacity,
            ReadFromOutbound));

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

        //var i = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            //_logger.LogInformation("MessageCount: {i}", ++i);
            var (_, anotherFrame) = await subSocket.ReceiveFrameStringAsync(stoppingToken);
            if (!anotherFrame)
            {
                continue;
            }
            var (bytes, _) = await subSocket.ReceiveFrameBytesAsync(stoppingToken);
            saveToChannel(bytes);
        }
    }

    private async Task PublisherAsync(CancellationToken stoppingToken, string host, int port, string topic, int maxCapacity,
        Func<CancellationToken, Task<ParameterSetMeshUpdateMessage>> readFromChannel)
    {
        _logger.LogInformation("Publisher binding on {Host}:{Port}, Topic: {Topic} ", host, port, topic);

        using var pubSocket = new PublisherSocket();
        pubSocket.Options.SendHighWatermark = maxCapacity;
        pubSocket.Bind($"tcp://{host}:{port}");

        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetMeshUpdateMessage = await readFromChannel(stoppingToken);
            _logger.LogInformation("sending: {Ticker} ", parameterSetMeshUpdateMessage.Ticker);
            pubSocket.SendMoreFrame(topic).SendFrame(parameterSetMeshUpdateMessage.ToByteArray());
        }
    }
    #endregion

    private async void WriteToInbound(byte[] bytes)
    {
        var msg = ParameterSetUpdateMessage.Parser.ParseFrom(bytes);
        await _inboundChannel.Writer.WriteAsync(msg);
    }

    private async Task<ParameterSetMeshUpdateMessage> ReadFromOutbound(CancellationToken stoppingToken)
    {
        return await _outboundChannel.Reader.ReadAsync(stoppingToken);
    }

    private async void ProcessMarketDataMessage(CancellationToken stoppingToken)
    {
        // pick the latest message
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetUpdateMessage = await _inboundChannel.Reader.ReadAsync(stoppingToken);
            var parameterSetMeshUpdateMessage = GetParameterSetMeshUpdateMessage(parameterSetUpdateMessage);
            await _outboundChannel.Writer.WriteAsync(parameterSetMeshUpdateMessage);
        }
    }

    private ParameterSetMeshUpdateMessage GetParameterSetMeshUpdateMessage(ParameterSetUpdateMessage parameterSetUpdateMessage)
    {
        //TODO: fill this
        //TODO: sample message below
        return new ParameterSetMeshUpdateMessage()
        {
            Ticker = parameterSetUpdateMessage.Ticker,
            PriceTime = parameterSetUpdateMessage.PriceTime,
        };
    }
}
