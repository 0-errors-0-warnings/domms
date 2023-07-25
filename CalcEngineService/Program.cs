using CalcEngineService;
using CalcEngineService.Caches;
using CalcEngineService.Configs;
using CalcEngineService.Handlers;

IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appsettings.json");

IConfigurationRoot configuration = configurationBuilder.Build();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        services.Configure<AppParamsConfiguration>(configuration.GetSection("AppParams"));
        services.Configure<List<MeshConfigSetConfiguration>>(configuration.GetSection("MeshConfigSet"));

        services.AddSingleton<IParameterSetUpdateCache, ParameterSetUpdateCache>();
        services.AddSingleton<IMeshConfigSetCache, MeshConfigSetCache>();
        services.AddSingleton<IServiceHandler, CalcEngineServiceHandler>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
