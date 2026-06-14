using System.Text.Json;
using HvacController.Models;

namespace HvacController.Services;

public sealed class OutsideWeatherClient : IOutsideWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly string _station;

    public OutsideWeatherClient(HttpClient httpClient, string station)
    {
        _httpClient = httpClient;
        _station = station;
    }

    public async Task<OutsideWeatherReading?> GetLatestAsync(
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/stations/{_station}/observations/latest");

        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "humidity-check-script/1.0 langdon@calvaryeagles.org");

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            return OutsideWeatherParser.Parse(json);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}