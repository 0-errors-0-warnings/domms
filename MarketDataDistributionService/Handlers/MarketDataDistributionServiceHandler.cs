using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MarketDataDistributionService.Configs;
using MarketDataDistributionService.Messages;
using MarketDataService.Messages;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using System.Threading.Channels;

namespace MarketDataDistributionService.Handlers;

public class MarketDataDistributionServiceHandler : IServiceHandler
{
    private readonly ILogger<MarketDataDistributionServiceHandler> _logger;
    private readonly AppParamsConfiguration _appParamsConfiguration;
    private readonly Dictionary<string, OptionStaticDataConfiguration> _optionStaticDataConfigurationDict;
    private Channel<MarketDataMessage> _inboundChannel;
    private Channel<ParameterSetUpdateMessage> _outboundChannel;
    private List<Task> _processorTaskList;

    public MarketDataDistributionServiceHandler(ILogger<MarketDataDistributionServiceHandler> logger, 
        IOptions<AppParamsConfiguration> appParamsConfigurationOption,
        IOptions<List<OptionStaticDataConfiguration>> optionStaticDataConfigurationListOption)
    {
        _logger = logger;
        _appParamsConfiguration = appParamsConfigurationOption.Value;
        _optionStaticDataConfigurationDict = optionStaticDataConfigurationListOption.Value.ToDictionary(x => x.Underlier);
        _inboundChannel = Channel.CreateBounded<MarketDataMessage>(_appParamsConfiguration.ReceiveMessageCapacity);
        _outboundChannel = Channel.CreateBounded<ParameterSetUpdateMessage>(_appParamsConfiguration.SendMessageCapacity);
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
            _appParamsConfiguration.ZeroMqSendTopicPrefix,
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

    private async Task PublisherAsync(CancellationToken stoppingToken, string host, int port, string topicPrefix, int maxCapacity,
        Func<CancellationToken, Task<ParameterSetUpdateMessage>> readFromChannel)
    {
        _logger.LogInformation("Publisher binding on {Host}:{Port}, TopicPrefix: {TopicPrefix}_<Instrument Name>", host, port, topicPrefix);

        using var pubSocket = new PublisherSocket();
        pubSocket.Options.SendHighWatermark = maxCapacity;
        pubSocket.Bind($"tcp://{host}:{port}");

        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetUpdateMessage = await readFromChannel(stoppingToken);
            var newTopic = $"{topicPrefix}_{parameterSetUpdateMessage.Underlier}";
            _logger.LogInformation("sending: {Underlier}, PriceTime: {PriceTime}, Topic: {newTopic}", 
                parameterSetUpdateMessage.Underlier, parameterSetUpdateMessage.PriceTime, newTopic);
            pubSocket.SendMoreFrame(newTopic).SendFrame(parameterSetUpdateMessage.ToByteArray());
        }
    }
    #endregion 

    private async void WriteToInbound(byte[] bytes)
    {
        var msg = MarketDataMessage.Parser.ParseFrom(bytes);
        await _inboundChannel.Writer.WriteAsync(msg);
    }

    private async Task<ParameterSetUpdateMessage> ReadFromOutbound(CancellationToken stoppingToken)
    {
        var msg = await _outboundChannel.Reader.ReadAsync(stoppingToken);
        return msg;
    }

    private async void ProcessMarketDataMessage(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            var marketDataMessage = await _inboundChannel.Reader.ReadAsync(stoppingToken);
            var parameterSetUpdateMessage = GetParameterSetUpdateMessage(marketDataMessage);
            await _outboundChannel.Writer.WriteAsync(parameterSetUpdateMessage);
        }
    }

    private ParameterSetUpdateMessage GetParameterSetUpdateMessage(MarketDataMessage marketDataMessage)
    {
        var optionStaticData = _optionStaticDataConfigurationDict[marketDataMessage.Underlier];
        return new ParameterSetUpdateMessage()
        {
            Underlier = marketDataMessage.Underlier,
            SpotPx = (marketDataMessage.BidPx + marketDataMessage.AskPx) / 2.0,
            VolatilityPct = marketDataMessage.VolatilityPct,
            RiskFreeRatePct = marketDataMessage.RiskFreeRatePct,
            DividendYieldPct = marketDataMessage.DividendYieldPct,
            ExpiryDate = DateTime.Today.AddDays(optionStaticData.OptionExpiryDateInDays).ToTimestamp(),
            StrikePrice = optionStaticData.StrikePrice,
            PriceTime = marketDataMessage.PriceTime
        };
    }
}