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
            _logger.LogInformation("sending: {Ticker} ", marketDataMessage.Ticker);

            pubSocket.SendMoreFrame(_appParamsConfiguration.ZeroMqSendTopic).SendFrame(marketDataMessage.ToByteArray());

            //await Task.Delay(10);
        }

        await Task.CompletedTask;
    }

    private MarketDataMessage GetMarketDataMessage()
    {
        var (ticker, bidPx, askPx, priceTime) = _priceGenerator.GetPrice();
        return new MarketDataMessage
        {
            Ticker = ticker,
            BidPx = bidPx,
            AskPx = askPx,
            PriceTime = priceTime.ToTimestamp(),
            DividendYieldPct = _divYieldGenerator.GetDivYieldPct(),
            RiskFreeRatePct = _riskFreeRateGenerator.GetRiskFreeRatePct(),
            VolatilityPct = _volGenerator.GetVolPct()
        };
    }
}