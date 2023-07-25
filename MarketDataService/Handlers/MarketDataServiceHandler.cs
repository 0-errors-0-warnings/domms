using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MarketDataService.Configs;
using MarketDataService.DataGenerators;
using MarketDataService.Messages;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;


namespace MarketDataService.Handlers;

public class MarketDataServiceHandler : IServiceHandler
{
    private readonly ILogger<MarketDataServiceHandler> _logger;
    private readonly AppParamsConfiguration _appParamsConfiguration;
    private readonly PriceGenerator _priceGenerator;
    private readonly VolGenerator _volGenerator;
    private readonly DivYieldGenerator _divYieldGenerator;
    private readonly RiskFreeRateGenerator _riskFreeRateGenerator;

    public MarketDataServiceHandler(ILogger<MarketDataServiceHandler> logger, IOptions<AppParamsConfiguration> appParamsConfigurationOption, 
        PriceGenerator priceGenerator, VolGenerator volGenerator, DivYieldGenerator divYieldGenerator, RiskFreeRateGenerator riskFreeRateGenerator)
    {
        _logger = logger;
        _appParamsConfiguration = appParamsConfigurationOption.Value;
        _priceGenerator = priceGenerator;
        _volGenerator = volGenerator;
        _divYieldGenerator = divYieldGenerator;
        _riskFreeRateGenerator = riskFreeRateGenerator;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        await PublisherAsync(stoppingToken);
    }

    private async Task PublisherAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Publisher binding on {Host}:{Port}, Topic: {Topic} ", 
            _appParamsConfiguration.ZeroMqHost, _appParamsConfiguration.ZeroMqSendPort, _appParamsConfiguration.ZeroMqSendTopic);

        using var pubSocket = new PublisherSocket();
        pubSocket.Options.SendHighWatermark = 1000;
        pubSocket.Bind($"tcp://{_appParamsConfiguration.ZeroMqHost}:{_appParamsConfiguration.ZeroMqSendPort}");

        while(!stoppingToken.IsCancellationRequested)
        {
            var marketDataMessage = GetMarketDataMessage();
            _logger.LogInformation("sending: {Underlier}, Price:{PriceTime} ", marketDataMessage.Underlier, marketDataMessage.PriceTime);

            pubSocket.SendMoreFrame(_appParamsConfiguration.ZeroMqSendTopic).SendFrame(marketDataMessage.ToByteArray());

            //TODO: Only for testing. Remove later.
            await Task.Delay(5*1000, stoppingToken);
        }

        await Task.CompletedTask;
    }

    private MarketDataMessage GetMarketDataMessage()
    {
        var (underlier, bidPx, askPx, priceTime) = _priceGenerator.GetPrice();
        return new MarketDataMessage
        {
            Underlier = underlier,
            BidPx = bidPx,
            AskPx = askPx,
            VolatilityPct = _volGenerator.GetVolPct(),
            RiskFreeRatePct = _riskFreeRateGenerator.GetRiskFreeRatePct(),
            DividendYieldPct = _divYieldGenerator.GetDivYieldPct(),
            PriceTime = priceTime.ToTimestamp()
        };
    }
}