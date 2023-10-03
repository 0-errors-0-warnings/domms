using CalcEngineService.Messages;
using NetMQ;
using NetMQ.Sockets;
using QuoteEngineService.Caches;
using QuoteEngineService.Configs;
using QuoteEngineService.Queues;
using System.Threading.Channels;

namespace QuoteEngineService.Handlers;

public class QuoteEngineServiceCalcEngineHandler : ICalcEngineServiceHandler
{
    private readonly ILogger<QuoteEngineServiceCalcEngineHandler> _logger;
    private readonly CalcEngineReceiveParamsConfiguration _calcEngineReceiveParamsConfiguration;
    private readonly IParameterCache<ParameterSetMeshUpdateMessage> _parameterSetMeshUpdateCache;
    private readonly Channel<ParameterSetMeshUpdateMessage> _inboundChannel;
    private readonly CustomMessageQueue<ParameterSetMeshUpdateMessage> _customMessageQueue = new();


    public QuoteEngineServiceCalcEngineHandler(ILogger<QuoteEngineServiceCalcEngineHandler> logger,
        CalcEngineReceiveParamsConfiguration calcEngineReceiveParamsConfiguration,
        IParameterCache<ParameterSetMeshUpdateMessage> parameterSetMeshUpdateCache)
    {
        _logger = logger;
        _calcEngineReceiveParamsConfiguration = calcEngineReceiveParamsConfiguration;
        _parameterSetMeshUpdateCache = parameterSetMeshUpdateCache;

        _inboundChannel = Channel.CreateBounded<ParameterSetMeshUpdateMessage>(_calcEngineReceiveParamsConfiguration.ReceiveMessageCapacity);
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        // start processor tasks
        var inboundChannelProcessor = Task.Run(() => ProcessInboundChannelMessages(stoppingToken), stoppingToken);
        var paramSetMeshUpdateProcessor = Task.Run(() => ProcessParameterMeshSetUpdateMessage(stoppingToken), stoppingToken);

        using var runtime = new NetMQRuntime();
        runtime.Run(stoppingToken,
            SubscriberAsync(_calcEngineReceiveParamsConfiguration.ZeroMqReceiveHost,
            _calcEngineReceiveParamsConfiguration.ZeroMqReceivePort,
            _calcEngineReceiveParamsConfiguration.ZeroMqReceiveTopic,
            _calcEngineReceiveParamsConfiguration.ReceiveMessageCapacity,
            WriteToInbound,
            stoppingToken));

        await Task.WhenAll(inboundChannelProcessor, paramSetMeshUpdateProcessor);
    }

    #region zeromq calls - abstract this out later

    private async Task SubscriberAsync(string host, int port, string topic, int maxCapacity, Action<byte[]> saveToChannel, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscriber binding on {Host}:{Port}, Topic: {Topic} ", host, port, topic);

        using var subSocket = new SubscriberSocket();
        subSocket.Options.ReceiveHighWatermark = maxCapacity;
        subSocket.Connect($"tcp://{host}:{port}");
        subSocket.Subscribe(topic);

        //var i = 0;
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
        var msg = ParameterSetMeshUpdateMessage.Parser.ParseFrom(bytes);
        await _inboundChannel.Writer.WriteAsync(msg);
    }

    private async void ProcessInboundChannelMessages(CancellationToken stoppingToken)
    {
        //pick the latest message
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetMeshUpdateMessage = await _inboundChannel.Reader.ReadAsync(stoppingToken);
            _customMessageQueue.Enqueue(parameterSetMeshUpdateMessage.Underlier, parameterSetMeshUpdateMessage);
        }
    }

    private void ProcessParameterMeshSetUpdateMessage(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetMeshUpdateMessage = _customMessageQueue.Dequeue();

            if (parameterSetMeshUpdateMessage == null)
            {
                continue;
            }

            // Update Parameter Set cache
            _parameterSetMeshUpdateCache.Update(parameterSetMeshUpdateMessage);
        }
    }
}
