using HvacController;
using HvacController.Services;

var builder = Host.CreateApplicationBuilder(args);

// Choose Development or Live-Run Mode below
// Development/no-GPIO mode
builder.Services.AddSingleton<IRelayService, NoOpRelayService>();
// Live Run Mode w/ real relays
// builder.Services.AddSingleton<IRelayService>(_ => new RelayService(activeHigh: false));

builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<OutsideWeatherState>();

builder.Services.AddSingleton<IOutsideWeatherClient>(_ =>
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri("https://api.weather.gov"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    return new OutsideWeatherClient(httpClient, "KMKG");
});

builder.Services.AddSingleton<IOutsideWeatherUpdater, OutsideWeatherUpdater>();

builder.Services.AddHostedService<OutsideWeatherBackgroundService>();

var host = builder.Build();
host.Run();
