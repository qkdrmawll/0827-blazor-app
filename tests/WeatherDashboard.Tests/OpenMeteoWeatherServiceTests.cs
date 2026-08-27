using System.Net;
using System.Text;
using WeatherDashboard.Weather;

namespace WeatherDashboard.Tests;

public sealed class OpenMeteoWeatherServiceTests
{
    [Fact]
    public async Task GetForecastAsync_MapsOpenMeteoResponses()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/v1/search", StringComparison.Ordinal))
            {
                Assert.Contains("name=Seoul", path);
                Assert.Contains("language=ko", path);
                return JsonResponse("""
                    {
                      "results": [{
                        "name": "서울",
                        "latitude": 37.566,
                        "longitude": 126.9784,
                        "country": "대한민국",
                        "admin1": "서울특별시",
                        "timezone": "Asia/Seoul"
                      }]
                    }
                    """);
            }

            Assert.Contains("wind_speed_unit=ms", path);
            Assert.Contains("forecast_days=5", path);
            return JsonResponse("""
                {
                  "utc_offset_seconds": 32400,
                  "current": {
                    "time": "2026-08-27T14:15",
                    "temperature_2m": 29.1,
                    "apparent_temperature": 31.4,
                    "relative_humidity_2m": 63,
                    "wind_speed_10m": 2.5,
                    "weather_code": 2
                  },
                  "hourly": {
                    "time": ["2026-08-27T14:00", "2026-08-27T15:00"],
                    "temperature_2m": [29.0, 28.4],
                    "precipitation_probability": [10, 20],
                    "weather_code": [2, 61]
                  },
                  "daily": {
                    "time": ["2026-08-27", "2026-08-28"],
                    "temperature_2m_max": [31.0, 30.0],
                    "temperature_2m_min": [24.0, 23.0],
                    "precipitation_probability_max": [30, 60],
                    "weather_code": [2, 61]
                  }
                }
                """);
        });
        var service = CreateService(handler);

        var forecast = await service.GetForecastAsync("Seoul");

        Assert.Equal("서울, 서울특별시, 대한민국", forecast.Location.DisplayName);
        Assert.Equal(29.1, forecast.Current.TemperatureCelsius);
        Assert.Equal(2.5, forecast.Current.WindSpeedMetersPerSecond);
        Assert.Equal(TimeSpan.FromHours(9), forecast.Current.Time.Offset);
        Assert.Single(forecast.Hourly);
        Assert.Equal(20, forecast.Hourly[0].PrecipitationProbabilityPercent);
        Assert.Equal(2, forecast.Daily.Count);
        Assert.Equal("비", forecast.Daily[1].Condition.Description);
    }

    [Fact]
    public async Task GetForecastAsync_ThrowsWhenLocationIsNotFound()
    {
        var service = CreateService(new StubHttpMessageHandler(
            _ => JsonResponse("""{}""")));

        var exception = await Assert.ThrowsAsync<WeatherLocationNotFoundException>(
            () => service.GetForecastAsync("missing"));

        Assert.Equal("missing", exception.Query);
    }

    [Fact]
    public async Task SearchLocationsAsync_NormalizesKoreanCityAlias()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Contains("name=Seoul", request.RequestUri!.Query);
            return JsonResponse("""
                {
                  "results": [{
                    "name": "서울특별시",
                    "latitude": 37.566,
                    "longitude": 126.9784,
                    "country": "대한민국"
                  }]
                }
                """);
        });
        var service = CreateService(handler);

        var locations = await service.SearchLocationsAsync("서울");

        Assert.Single(locations);
        Assert.Equal("서울특별시", locations[0].Name);
    }

    [Fact]
    public async Task SearchLocationsAsync_PropagatesHttpFailure()
    {
        var service = CreateService(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SearchLocationsAsync("Seoul"));
    }

    private static OpenMeteoWeatherService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        return new OpenMeteoWeatherService(new StubHttpClientFactory(client));
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
