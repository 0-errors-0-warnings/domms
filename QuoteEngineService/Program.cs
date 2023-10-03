using CalcEngineService.Messages;
using MarketDataDistributionService.Messages;
using QuoteEngineService;
using QuoteEngineService.Caches;
using QuoteEngineService.Configs;
using QuoteEngineService.Handlers;

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appsettings.json");

IConfigurationRoot configuration = configurationBuilder.Build();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        services.Configure<MddsReceiveParamsConfiguration>(configuration.GetSection("ZeroMqMddsReceiveParams"));
        services.Configure<List<CalcEngineReceiveParamsConfiguration>>(configuration.GetSection("ZeroMqCalcEngineReceiveParams"));
        services.Configure<List<AdminParamSetConfiguration>>(configuration.GetSection("AdminParamSetCache"));

        services.AddSingleton<IParameterCache<ParameterSetUpdateMessage>, ParameterCache<ParameterSetUpdateMessage>>();
        services.AddSingleton<IParameterCache<ParameterSetMeshUpdateMessage>, ParameterCache<ParameterSetMeshUpdateMessage>>();

        services.AddSingleton<IConfigParameterSetCache, ConfigParameterSetCache>();
        services.AddSingleton<IAdminParameterSetCache, AdminParameterSetCache>();

        services.AddSingleton<IOptionsPricingHandler, OptionsPricingHandler>();
        services.AddSingleton<IMddsServiceHandler, QuoteEngineServiceMddsHandler>();
        services.AddSingleton<IServiceHandler, QuoteEngineServiceAggregateHandler>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
