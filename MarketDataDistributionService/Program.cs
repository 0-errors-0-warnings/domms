using MarketDataDistributionService;
using MarketDataDistributionService.Configs;
using MarketDataDistributionService.Handlers;

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appsettings.json");

IConfigurationRoot configuration = configurationBuilder.Build();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        services.Configure<AppParamsConfiguration>(configuration.GetSection("AppParams"));
        services.Configure<ParameterSetRangesConfiguration>(configuration.GetSection("ParameterSetRanges"));

        services.AddSingleton<IServiceHandler, MarketDataDistributionServiceHandler>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
