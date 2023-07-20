using MarketDataService;
using MarketDataService.Configs;
using MarketDataService.DataGenerators;
using MarketDataService.Distributions;
using MarketDataService.Handlers;

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appsettings.json");

IConfigurationRoot configuration = configurationBuilder.Build();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        services.Configure<AppParamsConfiguration>(configuration.GetSection("AppParams"));
        services.Configure<OptionParamsConfiguration>(configuration.GetSection("OptionParams"));
        services.Configure<InstrumentConfiguration>(configuration.GetSection("Instrument"));
        services.Configure<VolatilityParamsConfiguration>(configuration.GetSection("VolatilityParams"));

        services.AddSingleton<IDistribution, UniformDistribution>();
        services.AddSingleton<VolGenerator>();
        services.AddSingleton<DivYieldGenerator>();
        services.AddSingleton<RiskFreeRateGenerator>();
        services.AddSingleton<PriceGenerator>();

        services.AddSingleton<IServiceHandler, MarketDataServiceHandler>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
