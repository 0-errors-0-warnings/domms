using MarketDataDistributionService.Messages;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using QuoteEngineService.Caches;
using QuoteEngineService.Configs;
using QuoteEngineService.Queues;
using System.Threading.Channels;

namespace QuoteEngineService.Handlers;

public class QuoteEngineServiceMddsHandler : IMddsServiceHandler
{
    private readonly ILogger<QuoteEngineServiceMddsHandler> _logger;
    private readonly MddsReceiveParamsConfiguration _mddsReceiveParamsConfiguration;
    private readonly Channel<ParameterSetUpdateMessage> _inboundChannel;
    private readonly CustomMessageQueue<ParameterSetUpdateMessage> _customMessageQueue = new();

    private IConfigParameterSetCache _configParameterSetCache;


    public QuoteEngineServiceMddsHandler(ILogger<QuoteEngineServiceMddsHandler> logger, 
        IOptions<MddsReceiveParamsConfiguration> mddsReceiveParamsConfigurationOption)
    {
        _logger = logger;
        _mddsReceiveParamsConfiguration = mddsReceiveParamsConfigurationOption.Value;
        _inboundChannel = Channel.CreateBounded<ParameterSetUpdateMessage>(_mddsReceiveParamsConfiguration.ReceiveMessageCapacity);
    }

    public async Task StartAsync(IConfigParameterSetCache configParameterSetCache, CancellationToken stoppingToken)
    {
        _configParameterSetCache = configParameterSetCache;

        // start processor tasks
        var inboundChannelProcessor = Task.Run(() => ProcessInboundChannelMessages(stoppingToken), stoppingToken);
        var paramSetProcessor = Task.Run(() => ProcessParameterSetUpdateMessage(stoppingToken), stoppingToken);

        using var runtime = new NetMQRuntime();
        runtime.Run(stoppingToken,
            SubscriberAsync(_mddsReceiveParamsConfiguration.ZeroMqReceiveHost,
            _mddsReceiveParamsConfiguration.ZeroMqReceivePort,
            _mddsReceiveParamsConfiguration.ZeroMqReceiveTopic,
            _mddsReceiveParamsConfiguration.ReceiveMessageCapacity,
            WriteToInbound,
            stoppingToken));

        await Task.WhenAll(inboundChannelProcessor, paramSetProcessor);
    }

    #region zeromq calls - abstract this out later

    private async Task SubscriberAsync(string host, int port, string topic, int maxCapacity, Action<byte[]> saveToChannel, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscriber binding on {Host}:{Port}, Topic: {Topic} ", host, port, topic);

        using var subSocket = new SubscriberSocket();
        subSocket.Options.ReceiveHighWatermark = maxCapacity;
        subSocket.Connect($"tcp://{host}:{port}");
        subSocket.Subscribe(topic);

        while (!stoppingToken.IsCancellationRequested)
        {
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
        var msg = ParameterSetUpdateMessage.Parser.ParseFrom(bytes);
        await _inboundChannel.Writer.WriteAsync(msg);
    }

    private async void ProcessInboundChannelMessages(CancellationToken stoppingToken)
    {
        //pick the latest message
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetUpdateMessage = await _inboundChannel.Reader.ReadAsync(stoppingToken);
            _customMessageQueue.Enqueue(parameterSetUpdateMessage.Underlier, parameterSetUpdateMessage);
        }
    }

    private void ProcessParameterSetUpdateMessage(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetUpdateMessage = _customMessageQueue.Dequeue();

            if (parameterSetUpdateMessage == null)
            {
                continue;
            }

            //_logger.LogInformation("MDDS:  {Underlier} ", parameterSetUpdateMessage.Underlier);
            // Update Parameter Set cache
            _configParameterSetCache.UpdateConfigSet(parameterSetUpdateMessage);
        }
    }
}
