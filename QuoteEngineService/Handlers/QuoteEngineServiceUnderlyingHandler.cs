using CalcEngineService.Messages;
using NetMQ;
using NetMQ.Sockets;
using QuoteEngineService.Configs;
using System.Threading.Channels;

namespace QuoteEngineService.Handlers;

public class QuoteEngineServiceUnderlyingHandler : IUnderlyingServiceHandler
{
    private readonly ILogger<QuoteEngineServiceUnderlyingHandler> _logger;
    private readonly ZeroMqReceiveParamsConfiguration _zeroMqReceiveParamsConfiguration;
    private Channel<ParameterSetMeshUpdateMessage> _inboundChannel;
    private List<Task> _processorTaskList;

    public QuoteEngineServiceUnderlyingHandler(ILogger<QuoteEngineServiceUnderlyingHandler> logger, 
        ZeroMqReceiveParamsConfiguration zeroMqReceiveParamsConfiguration)
    {
        _logger = logger;
        _zeroMqReceiveParamsConfiguration = zeroMqReceiveParamsConfiguration;
        _inboundChannel = Channel.CreateBounded<ParameterSetMeshUpdateMessage>(_zeroMqReceiveParamsConfiguration.ReceiveMessageCapacity);
        _processorTaskList = new List<Task>();
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        // start processor tasks
        for (int i = 0; i < _zeroMqReceiveParamsConfiguration.NumOfThreads; i++)
        {
            _processorTaskList.Add(Task.Run(() => ProcessCalcEngineMessage(stoppingToken), stoppingToken));
        }


        using var runtime = new NetMQRuntime();
        runtime.Run(stoppingToken,
            SubscriberAsync(stoppingToken,
            _zeroMqReceiveParamsConfiguration.ZeroMqReceiveHost,
            _zeroMqReceiveParamsConfiguration.ZeroMqReceivePort,
            _zeroMqReceiveParamsConfiguration.ZeroMqReceiveTopic,
            _zeroMqReceiveParamsConfiguration.ReceiveMessageCapacity,
            WriteToInbound));

        //await Task.WhenAll(_processorTaskList);
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

    #endregion

    private async void WriteToInbound(byte[] bytes)
    {
        var msg = ParameterSetMeshUpdateMessage.Parser.ParseFrom(bytes);
        await _inboundChannel.Writer.WriteAsync(msg);
    }


    int count = 0;
    private async void ProcessCalcEngineMessage(CancellationToken stoppingToken)
    {
        // pick the latest message
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetMeshUpdateMessage = await _inboundChannel.Reader.ReadAsync(stoppingToken);

            count++;

            if (count % 50000 == 0)
            {
                _logger.LogInformation("parameterSetMeshUpdateMessage: {Ticker}, PriceTime: {PriceTime}",
                    parameterSetMeshUpdateMessage.Ticker, parameterSetMeshUpdateMessage.PriceTime);
            }
            //TODO: update PS via double buffering
            //TODO: publish
        }
    }


}
