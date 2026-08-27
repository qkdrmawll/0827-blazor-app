namespace WeatherDashboard.Weather;

public interface IWeatherService
{
    Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(
        string query,
        int count = 5,
        CancellationToken cancellationToken = default);

    Task<WeatherForecast> GetForecastAsync(
        WeatherLocation location,
        CancellationToken cancellationToken = default);

    Task<WeatherForecast> GetForecastAsync(
        string query,
        CancellationToken cancellationToken = default);
}

public sealed class WeatherLocationNotFoundException(string query)
    : Exception($"No location was found for '{query}'.")
{
    public string Query { get; } = query;
}
