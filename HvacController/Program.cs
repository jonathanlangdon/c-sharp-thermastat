using HvacController;
using HvacController.Services;

var builder = Host.CreateApplicationBuilder(args);
// builder.Services.AddHostedService<Worker>();

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
