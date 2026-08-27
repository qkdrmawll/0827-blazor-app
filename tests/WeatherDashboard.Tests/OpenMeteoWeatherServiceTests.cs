using System.Net;
using System.Text;
using System.Text.Json;
using WeatherDashboard.Weather;

namespace WeatherDashboard.Tests;

public sealed class OpenMeteoWeatherServiceTests
{
    [Fact]
    public async Task GetForecastAsync_MapsCurrentNextEightHoursAndFiveDays()
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
                    "time": [
                      "2026-08-27T13:00", "2026-08-27T14:00", "2026-08-27T15:00",
                      "2026-08-27T16:00", "2026-08-27T17:00", "2026-08-27T18:00",
                      "2026-08-27T19:00", "2026-08-27T20:00", "2026-08-27T21:00",
                      "2026-08-27T22:00", "2026-08-27T23:00"
                    ],
                    "temperature_2m": [30.0, 29.0, 28.4, 28.0, 27.5, 27.0, 26.5, 26.0, 25.5, 25.0, 24.5],
                    "precipitation_probability": [0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100],
                    "weather_code": [0, 1, 2, 3, 45, 51, 61, 71, 80, 95, 99]
                  },
                  "daily": {
                    "time": ["2026-08-27", "2026-08-28", "2026-08-29", "2026-08-30", "2026-08-31"],
                    "temperature_2m_max": [31.0, 30.0, 29.0, 28.0, 27.0],
                    "temperature_2m_min": [24.0, 23.0, 22.0, 21.0, 20.0],
                    "precipitation_probability_max": [30, 60, 20, 10, 80],
                    "weather_code": [2, 61, 0, 3, 95]
                  }
                }
                """);
        });
        var service = CreateService(handler);

        var forecast = await service.GetForecastAsync("Seoul");

        Assert.Equal("서울, 서울특별시, 대한민국", forecast.Location.DisplayName);
        Assert.Equal(29.1, forecast.Current.TemperatureCelsius);
        Assert.Equal(31.4, forecast.Current.ApparentTemperatureCelsius);
        Assert.Equal(63, forecast.Current.RelativeHumidityPercent);
        Assert.Equal(2.5, forecast.Current.WindSpeedMetersPerSecond);
        Assert.Equal(TimeSpan.FromHours(9), forecast.Current.Time.Offset);
        Assert.Equal("부분적으로 흐림", forecast.Current.Condition.Description);
        Assert.Equal(8, forecast.Hourly.Count);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 27, 15, 0, 0, TimeSpan.FromHours(9)),
            forecast.Hourly[0].Time);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 27, 22, 0, 0, TimeSpan.FromHours(9)),
            forecast.Hourly[^1].Time);
        Assert.Equal(20, forecast.Hourly[0].PrecipitationProbabilityPercent);
        Assert.Equal(5, forecast.Daily.Count);
        Assert.Equal(new DateOnly(2026, 8, 31), forecast.Daily[^1].Date);
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

    [Theory]
    [InlineData("New York", "name=New%20York")]
    [InlineData("성남시", "name=%EC%84%B1%EB%82%A8%EC%8B%9C")]
    public async Task SearchLocationsAsync_UrlEncodesEnglishAndKoreanQueries(
        string query,
        string expectedQuery)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Contains(expectedQuery, request.RequestUri!.OriginalString);
            return JsonResponse("""{"results": []}""");
        });
        var service = CreateService(handler);

        var locations = await service.SearchLocationsAsync(query);

        Assert.Empty(locations);
    }

    [Fact]
    public async Task SearchLocationsAsync_PropagatesHttpFailure()
    {
        var service = CreateService(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SearchLocationsAsync("Seoul"));
    }

    [Fact]
    public async Task SearchLocationsAsync_ThrowsForMalformedJson()
    {
        var service = CreateService(new StubHttpMessageHandler(
            _ => JsonResponse("""{"results": [""")));

        await Assert.ThrowsAsync<JsonException>(
            () => service.SearchLocationsAsync("Seoul"));
    }

    [Fact]
    public async Task GetForecastAsync_ThrowsForInconsistentForecastArrays()
    {
        var service = CreateService(new StubHttpMessageHandler(
            _ => JsonResponse("""
                {
                  "utc_offset_seconds": 0,
                  "current": {
                    "time": "2026-08-27T14:00",
                    "temperature_2m": 20,
                    "apparent_temperature": 20,
                    "relative_humidity_2m": 50,
                    "wind_speed_10m": 3,
                    "weather_code": 0
                  },
                  "hourly": {
                    "time": ["2026-08-27T14:00"],
                    "temperature_2m": [],
                    "precipitation_probability": [0],
                    "weather_code": [0]
                  },
                  "daily": {
                    "time": ["2026-08-27"],
                    "temperature_2m_max": [25],
                    "temperature_2m_min": [15],
                    "precipitation_probability_max": [0],
                    "weather_code": [0]
                  }
                }
                """)));
        var location = new WeatherLocation(
            "Seoul",
            "Seoul, South Korea",
            37.566,
            126.9784,
            "South Korea",
            null,
            "Asia/Seoul");

        await Assert.ThrowsAsync<JsonException>(
            () => service.GetForecastAsync(location));
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
