using QuoteEngineService;
using QuoteEngineService.Configs;
using QuoteEngineService.Handlers;

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appsettings.json");

IConfigurationRoot configuration = configurationBuilder.Build();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        services.Configure<ZeroMqReceiveParamsConfiguration>(configuration.GetSection("ZeroMqMddsReceiveParams"));
        services.Configure<List<ZeroMqReceiveParamsConfiguration>>(configuration.GetSection("ZeroMqCalcEngineReceiveParams"));

        services.AddSingleton<IMddsServiceHandler, QuoteEngineServiceMddsHandler>();
        //services.AddSingleton<IUnderlyingServiceHandler, QuoteEngineServiceUnderlyingHandler>();
        services.AddSingleton<IServiceHandler, QuoteEngineServiceAggregateHandler>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
