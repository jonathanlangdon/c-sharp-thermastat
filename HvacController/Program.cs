using HvacController;
using HvacController.Models;
using HvacController.Services;

var builder = Host.CreateApplicationBuilder(args);

var appSettings = builder.Configuration
    .GetSection("AppSettings")
    .Get<AppSettings>() ?? new AppSettings();

var runStatus = appSettings.RunStatus.Trim().ToLowerInvariant();

if (runStatus == "live")
{
    builder.Services.AddSingleton<IRelayService>(_ => new RelayService(activeHigh: false));
    builder.Services.AddSingleton<IDownstairsSensorReader, Sht45DownstairsSensorReader>();
}
else
{
    builder.Services.AddSingleton<IRelayService, NoOpRelayService>();
    builder.Services.AddSingleton<IDownstairsSensorReader, NoOpDownstairsSensorReader>();
}

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<OutsideWeatherBackgroundService>();
builder.Services.AddHostedService<UpstairsSensorMqttSubscriber>();
builder.Services.AddHostedService<DownstairsSensorBackgroundService>();

// MQTT status publishing is safe in both dev and live modes.
builder.Services.AddSingleton<IMqttMessagePublisher, MqttMessagePublisher>();
builder.Services.AddSingleton<IThermostatStatusPublisher, MqttThermostatStatusPublisher>();
builder.Services.AddSingleton<IndoorSensorState>();
builder.Services.AddSingleton<ThermostatInputBuilder>();
builder.Services.AddSingleton(new HvacSettings());
builder.Services.AddSingleton<ThermostatCycleRunner>();
builder.Services.AddSingleton<UpstairsSensorMessageHandler>();
builder.Services.AddSingleton<IDownstairsSensorUpdater, DownstairsSensorUpdater>();
builder.Services.AddSingleton<OutsideWeatherState>();
builder.Services.AddSingleton<IOutsideWeatherUpdater, OutsideWeatherUpdater>();
builder.Services.AddSingleton<IOutsideWeatherClient>(_ =>
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri("https://api.weather.gov"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    return new OutsideWeatherClient(httpClient, "KMKG");
});


var host = builder.Build();
host.Run();
