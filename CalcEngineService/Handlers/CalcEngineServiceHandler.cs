using CalcEngineService.Caches;
using CalcEngineService.Calculators;
using CalcEngineService.Configs;
using CalcEngineService.Messages;
using CalcEngineService.Queues;
using Google.Protobuf;
using MarketDataDistributionService.Messages;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using System.Diagnostics;
using System.Threading.Channels;

namespace CalcEngineService.Handlers;

public class CalcEngineServiceHandler : IServiceHandler
{
    private readonly ILogger<CalcEngineServiceHandler> _logger;
    private readonly AppParamsConfiguration _appParamsConfiguration;
    private readonly List<MeshConfigSetConfiguration> _meshConfigSetConfiguration;
    private readonly IParameterSetUpdateCache _parameterSetUpdateCache;
    private readonly IMeshConfigSetCache _meshConfigSetCache;
    private Channel<ParameterSetUpdateMessage> _inboundChannel;
    private Channel<ParameterSetMeshUpdateMessage> _outboundChannel;
    private readonly CustomMessageQueue<ParameterSetUpdateMessage> _customMessageQueue = new();
    private readonly Stopwatch _stopwatch = new();


    public CalcEngineServiceHandler(ILogger<CalcEngineServiceHandler> logger, 
        IOptions<AppParamsConfiguration> appParamsConfigurationOption,
        IOptions<List<MeshConfigSetConfiguration>> meshConfigSetConfiguration,
        IParameterSetUpdateCache parameterSetUpdateCache,
        IMeshConfigSetCache meshConfigSetCache)
    {
        _logger = logger;
        _appParamsConfiguration = appParamsConfigurationOption.Value;
        _meshConfigSetConfiguration = meshConfigSetConfiguration.Value;
        _parameterSetUpdateCache = parameterSetUpdateCache;

        _inboundChannel = Channel.CreateBounded<ParameterSetUpdateMessage>(_appParamsConfiguration.ReceiveMessageCapacity);
        _outboundChannel = Channel.CreateBounded<ParameterSetMeshUpdateMessage>(_appParamsConfiguration.SendMessageCapacity);

        _meshConfigSetCache = meshConfigSetCache;
        BuildInitialParameterMeshConfigSet();
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        // start processor tasks
        var inboundChannelProcessor = Task.Run(() => ProcessInboundChannelMessages(stoppingToken), stoppingToken);
        var paramSetProcessor = Task.Run(() => ProcessParameterSetUpdateMessage(stoppingToken), stoppingToken);

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
            _appParamsConfiguration.ZeroMqSendTopic,
            _appParamsConfiguration.SendMessageCapacity,
            ReadFromOutbound));

        await Task.WhenAll(inboundChannelProcessor, paramSetProcessor);
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
            _logger.LogInformation("sending: {Underlier}, Items: {Count}, PriceTime:{PriceTime}", parameterSetMeshUpdateMessage.Underlier, 
                parameterSetMeshUpdateMessage.ParameterSetList.Count, parameterSetMeshUpdateMessage.PriceTime);
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


    private async void ProcessInboundChannelMessages(CancellationToken stoppingToken)
    {
        //pick the latest message
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetUpdateMessage = await _inboundChannel.Reader.ReadAsync(stoppingToken);
            _customMessageQueue.Enqueue(parameterSetUpdateMessage.Underlier, parameterSetUpdateMessage);
        }
    }


    private async void ProcessParameterSetUpdateMessage(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var parameterSetUpdateMessage = _customMessageQueue.Dequeue();

            if(parameterSetUpdateMessage == null)
            {
                continue;
            }

            _stopwatch.Restart();
            // Update Parameter Set cache
            _parameterSetUpdateCache.UpdateParameterSet(parameterSetUpdateMessage);

            var parameterSetMeshUpdateMessage = GetParameterSetMeshUpdateMessage(parameterSetUpdateMessage);
            _stopwatch.Stop();

            _logger.LogInformation("Processed: {Underlier}, Items: {Count}, TimeTaken: {TimeTaken} ms", parameterSetMeshUpdateMessage.Underlier,
                parameterSetMeshUpdateMessage.ParameterSetList.Count, _stopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000);

            await _outboundChannel.Writer.WriteAsync(parameterSetMeshUpdateMessage);
        }
    }

    private ParameterSetMeshUpdateMessage GetParameterSetMeshUpdateMessage(ParameterSetUpdateMessage parameterSetUpdateMessage)
    {
        ParameterSetMeshUpdateMessage result = new()
        {
            Underlier = parameterSetUpdateMessage.Underlier,
            PriceTime = parameterSetUpdateMessage.PriceTime
        };

        PopulateParameterSetDimensions(result, parameterSetUpdateMessage);

        PopulateOptionPrices(result);

        return result;
    }

    private void PopulateParameterSetDimensions(ParameterSetMeshUpdateMessage result, ParameterSetUpdateMessage parameterSetUpdateMessage)
    {
        //top of the list should be current values
        var firstParamSet = new ParameterSet()
        {
            Id = 0,
            SpotPx = parameterSetUpdateMessage.SpotPx,
            VolatilityPct = parameterSetUpdateMessage.VolatilityPct,
            RiskFreeRatePct = parameterSetUpdateMessage.RiskFreeRatePct,
            DividendYieldPct = parameterSetUpdateMessage.DividendYieldPct,
            MaturityTimeYrs = (parameterSetUpdateMessage.ExpiryDate.ToDateTime() - DateTime.Today).TotalDays / 365.0,
            StrikePrice = parameterSetUpdateMessage.StrikePrice,
            CallValuationResults = new ValuationResults(),
            PutValuationResults = new ValuationResults(),
        };

        result.ParameterSetList.Add(firstParamSet);

        //now iterate over all
        PopulateRemainingValues(result, parameterSetUpdateMessage);
    }

    private void PopulateRemainingValues(ParameterSetMeshUpdateMessage result, ParameterSetUpdateMessage parameterSetUpdateMessage)
    {
        var underlierConfigSet = _meshConfigSetCache.GetCurrentConfigSet(parameterSetUpdateMessage.Underlier);
        if (underlierConfigSet == null)
        {
            _logger.LogError("underlierConfigSet not found for {Underlier}", result.Underlier);
            return;
        }

        var currentSpotPx = parameterSetUpdateMessage.SpotPx;
        var currentVolatilityPct = parameterSetUpdateMessage.VolatilityPct;
        var currentRiskFreeRatePct = parameterSetUpdateMessage.RiskFreeRatePct;
        var currentDividendYieldPct = parameterSetUpdateMessage.DividendYieldPct;
        var currentTime = (parameterSetUpdateMessage.ExpiryDate.ToDateTime() - DateTime.Today).TotalDays/365.0;
        var arrayIndex = 1;

        var spotHalfRange = underlierConfigSet.SpotRangePct / 2.0;
        var lowerSpot = currentSpotPx - spotHalfRange;
        var upperSpot = currentSpotPx + spotHalfRange;
        result.SpotMeshParamVector.Clear();
        for (var newSpotPx = lowerSpot; newSpotPx <= upperSpot; newSpotPx += underlierConfigSet.SpotStepSizePct)
        {
            if (newSpotPx <= 0)
            {
                continue;
            }

            result.SpotMeshParamVector.Add(newSpotPx);
            var volHalfRange = underlierConfigSet.VolRangePct / 2.0;
            var lowerVol = currentVolatilityPct - volHalfRange;
            var upperVol = currentVolatilityPct + volHalfRange;
            result.VolMeshParamVector.Clear();
            for (var newVolPct = lowerVol; newVolPct <= upperVol; newVolPct += underlierConfigSet.VolStepSizePct)
            {
                if (newVolPct < 0)
                {
                    continue;
                }

                result.VolMeshParamVector.Add(newVolPct);
                var rateHalfRange = underlierConfigSet.RateRangePct / 2.0;
                var lowerRate = currentRiskFreeRatePct - rateHalfRange;
                var upperRate = currentRiskFreeRatePct + rateHalfRange;
                result.RateMeshParamVector.Clear();
                for (var newRatePct = lowerRate; newRatePct <= upperRate; newRatePct += underlierConfigSet.RateStepSizePct)
                {
                    if (newRatePct < 0)
                    {
                        continue;
                    }

                    result.RateMeshParamVector.Add(newRatePct);
                    var paramSet = new ParameterSet()
                    {
                        Id = arrayIndex++,
                        SpotPx = newSpotPx,
                        VolatilityPct = newVolPct,
                        RiskFreeRatePct = newRatePct,
                        DividendYieldPct = currentDividendYieldPct,
                        MaturityTimeYrs = currentTime,
                        CallValuationResults = new ValuationResults(),
                        PutValuationResults = new ValuationResults(),
                    };

                    result.ParameterSetList.Add(paramSet);
                }
            }
        }
    }

    private void PopulateOptionPrices(ParameterSetMeshUpdateMessage result)
    {
        Parallel.ForEach(result.ParameterSetList, OptionPriceCalculator.CalculateOptionPriceAndGreeks);
    }

    private void BuildInitialParameterMeshConfigSet()
    {
        foreach(var underlierConfig in _meshConfigSetConfiguration)
        {
            _meshConfigSetCache.UpdateConfigSet(underlierConfig);
        }
    }
}
