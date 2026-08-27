using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherDashboard.Weather;

public sealed class OpenMeteoWeatherService(IHttpClientFactory httpClientFactory) : IWeatherService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, string> KoreanCityAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["서울"] = "Seoul",
            ["서울특별시"] = "Seoul",
            ["부산"] = "Busan",
            ["부산광역시"] = "Busan",
            ["대구"] = "Daegu",
            ["대구광역시"] = "Daegu",
            ["인천"] = "Incheon",
            ["인천광역시"] = "Incheon",
            ["광주"] = "Gwangju",
            ["광주광역시"] = "Gwangju",
            ["대전"] = "Daejeon",
            ["대전광역시"] = "Daejeon",
            ["울산"] = "Ulsan",
            ["울산광역시"] = "Ulsan",
            ["세종"] = "Sejong",
            ["세종특별자치시"] = "Sejong",
            ["수원"] = "Suwon",
            ["제주"] = "Jeju",
            ["제주시"] = "Jeju",
            ["춘천"] = "Chuncheon",
            ["강릉"] = "Gangneung",
            ["전주"] = "Jeonju",
            ["청주"] = "Cheongju",
            ["천안"] = "Cheonan",
            ["포항"] = "Pohang",
            ["창원"] = "Changwon",
            ["김해"] = "Gimhae"
        };

    public async Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(
        string query,
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 100);

        var client = httpClientFactory.CreateClient(WeatherServiceCollectionExtensions.GeocodingClientName);
        var normalizedQuery = NormalizeLocationQuery(query);
        var path = $"v1/search?name={Uri.EscapeDataString(normalizedQuery)}" +
            $"&count={count}&language=ko&format=json";
        using var response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GeocodingResponse>(
            JsonOptions,
            cancellationToken);
        if (payload is null)
        {
            throw new JsonException("The Open-Meteo geocoding response was empty.");
        }

        return payload.Results?.Select(MapLocation).ToArray() ?? [];
    }

    private static string NormalizeLocationQuery(string query)
    {
        var trimmedQuery = query.Trim();
        return KoreanCityAliases.GetValueOrDefault(trimmedQuery, trimmedQuery);
    }

    public async Task<WeatherForecast> GetForecastAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var locations = await SearchLocationsAsync(query, 1, cancellationToken);
        if (locations.Count == 0)
        {
            throw new WeatherLocationNotFoundException(query);
        }

        return await GetForecastAsync(locations[0], cancellationToken);
    }

    public async Task<WeatherForecast> GetForecastAsync(
        WeatherLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var client = httpClientFactory.CreateClient(WeatherServiceCollectionExtensions.ForecastClientName);
        var latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);
        var longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);
        var path = $"v1/forecast?latitude={latitude}&longitude={longitude}" +
            "&current=temperature_2m,apparent_temperature,relative_humidity_2m,wind_speed_10m,weather_code" +
            "&hourly=temperature_2m,precipitation_probability,weather_code" +
            "&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max,weather_code" +
            "&wind_speed_unit=ms&timezone=auto&forecast_days=5";
        using var response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ForecastResponse>(
            JsonOptions,
            cancellationToken);
        if (payload is null)
        {
            throw new JsonException("The Open-Meteo forecast response was empty.");
        }

        return MapForecast(location, payload);
    }

    private static WeatherLocation MapLocation(GeocodingResult result)
    {
        var displayParts = new[] { result.Name, result.Admin1, result.Country }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return new WeatherLocation(
            result.Name,
            string.Join(", ", displayParts),
            result.Latitude,
            result.Longitude,
            result.Country,
            result.Admin1,
            result.Timezone);
    }

    private static WeatherForecast MapForecast(
        WeatherLocation location,
        ForecastResponse response)
    {
        var current = response.Current ??
            throw new JsonException("The forecast response does not contain current weather.");
        var hourly = response.Hourly ??
            throw new JsonException("The forecast response does not contain hourly weather.");
        var daily = response.Daily ??
            throw new JsonException("The forecast response does not contain daily weather.");
        var offset = TimeSpan.FromSeconds(response.UtcOffsetSeconds);
        var currentTime = ParseLocalDateTime(current.Time, offset);

        EnsureSameLength(
            "hourly",
            hourly.Time.Length,
            hourly.Temperature.Length,
            hourly.PrecipitationProbability.Length,
            hourly.WeatherCode.Length);
        EnsureSameLength(
            "daily",
            daily.Time.Length,
            daily.MaximumTemperature.Length,
            daily.MinimumTemperature.Length,
            daily.PrecipitationProbability.Length,
            daily.WeatherCode.Length);

        var hourlyForecast = Enumerable.Range(0, hourly.Time.Length)
            .Select(index => new HourlyWeather(
                ParseLocalDateTime(hourly.Time[index], offset),
                hourly.Temperature[index],
                hourly.PrecipitationProbability[index],
                WmoWeatherCodeMapper.Map(hourly.WeatherCode[index])))
            .Where(item => item.Time >= currentTime)
            .Take(8)
            .ToArray();
        var dailyForecast = Enumerable.Range(0, daily.Time.Length)
            .Select(index => new DailyWeather(
                DateOnly.ParseExact(daily.Time[index], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                daily.MaximumTemperature[index],
                daily.MinimumTemperature[index],
                daily.PrecipitationProbability[index],
                WmoWeatherCodeMapper.Map(daily.WeatherCode[index])))
            .ToArray();

        return new WeatherForecast(
            location,
            new CurrentWeather(
                currentTime,
                current.Temperature,
                current.ApparentTemperature,
                current.RelativeHumidity,
                current.WindSpeed,
                WmoWeatherCodeMapper.Map(current.WeatherCode)),
            hourlyForecast,
            dailyForecast);
    }

    private static DateTimeOffset ParseLocalDateTime(string value, TimeSpan offset)
    {
        var localTime = DateTime.ParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
        return new DateTimeOffset(localTime, offset);
    }

    private static void EnsureSameLength(string section, params int[] lengths)
    {
        if (lengths.Distinct().Count() != 1)
        {
            throw new JsonException($"The {section} forecast arrays have inconsistent lengths.");
        }
    }

    private sealed record GeocodingResponse(
        [property: JsonPropertyName("results")] GeocodingResult[]? Results);

    private sealed record GeocodingResult(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude,
        [property: JsonPropertyName("country")] string Country,
        [property: JsonPropertyName("admin1")] string? Admin1,
        [property: JsonPropertyName("timezone")] string? Timezone);

    private sealed record ForecastResponse(
        [property: JsonPropertyName("utc_offset_seconds")] int UtcOffsetSeconds,
        [property: JsonPropertyName("current")] CurrentResponse? Current,
        [property: JsonPropertyName("hourly")] HourlyResponse? Hourly,
        [property: JsonPropertyName("daily")] DailyResponse? Daily);

    private sealed record CurrentResponse(
        [property: JsonPropertyName("time")] string Time,
        [property: JsonPropertyName("temperature_2m")] double Temperature,
        [property: JsonPropertyName("apparent_temperature")] double ApparentTemperature,
        [property: JsonPropertyName("relative_humidity_2m")] int RelativeHumidity,
        [property: JsonPropertyName("wind_speed_10m")] double WindSpeed,
        [property: JsonPropertyName("weather_code")] int WeatherCode);

    private sealed record HourlyResponse(
        [property: JsonPropertyName("time")] string[] Time,
        [property: JsonPropertyName("temperature_2m")] double[] Temperature,
        [property: JsonPropertyName("precipitation_probability")] int[] PrecipitationProbability,
        [property: JsonPropertyName("weather_code")] int[] WeatherCode);

    private sealed record DailyResponse(
        [property: JsonPropertyName("time")] string[] Time,
        [property: JsonPropertyName("temperature_2m_max")] double[] MaximumTemperature,
        [property: JsonPropertyName("temperature_2m_min")] double[] MinimumTemperature,
        [property: JsonPropertyName("precipitation_probability_max")] int[] PrecipitationProbability,
        [property: JsonPropertyName("weather_code")] int[] WeatherCode);
}
