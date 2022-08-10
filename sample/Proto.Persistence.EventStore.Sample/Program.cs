using Proto.Persistence.EventStore.Sample;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        services => {
            services.AddEventStoreClient("esdb://localhost:2113?tls=false");
            services.AddHostedService<Worker>();
        }
    )
    .Build();

Proto.Log.SetLoggerFactory(
    LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(LogLevel.Information))
);

await host.RunAsync();
