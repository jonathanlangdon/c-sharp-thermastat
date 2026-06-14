using System.Net;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class OutsideWeatherClientTests
{
    [Fact]
    public async Task GetLatestAsync_WhenWeatherGovReturnsValidObservation_ReturnsReading()
    {
        var json = """
        {
          "properties": {
            "temperature": {
              "value": 20.0
            },
            "relativeHumidity": {
              "value": 50.0
            }
          }
        }
        """;

        var httpClient = new HttpClient(new FakeHttpMessageHandler(json))
        {
            BaseAddress = new Uri("https://api.weather.gov")
        };

        var client = new OutsideWeatherClient(httpClient, "KMKG");

        var result = await client.GetLatestAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(68.0, result.OutsideTemperature);
        Assert.Equal(8.64, result.OutsideAbsoluteHumidity);
    }

    [Fact]
    public async Task GetLatestAsync_WhenWeatherGovReturnsInvalidObservation_ReturnsNull()
    {
        var json = """
        {
          "properties": {
            "temperature": {
              "value": null
            },
            "relativeHumidity": {
              "value": 50.0
            }
          }
        }
        """;

        var httpClient = new HttpClient(new FakeHttpMessageHandler(json))
        {
            BaseAddress = new Uri("https://api.weather.gov")
        };

        var client = new OutsideWeatherClient(httpClient, "KMKG");

        var result = await client.GetLatestAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestAsync_UsesConfiguredStation()
    {
        var json = """
        {
          "properties": {
            "temperature": {
              "value": 20.0
            },
            "relativeHumidity": {
              "value": 50.0
            }
          }
        }
        """;

        var handler = new FakeHttpMessageHandler(json);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.weather.gov")
        };

        var client = new OutsideWeatherClient(httpClient, "KMKG");

        await client.GetLatestAsync(CancellationToken.None);

        Assert.Equal(
            "https://api.weather.gov/stations/KMKG/observations/latest",
            handler.LastRequestUri?.ToString());
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public FakeHttpMessageHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody)
            });
        }
    }
}