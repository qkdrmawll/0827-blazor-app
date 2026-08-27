namespace WeatherDashboard.Weather;

public static class WeatherServiceCollectionExtensions
{
    public const string GeocodingClientName = "OpenMeteo.Geocoding";
    public const string ForecastClientName = "OpenMeteo.Forecast";

    public static IServiceCollection AddOpenMeteoWeather(
        this IServiceCollection services,
        TimeSpan? timeout = null)
    {
        var requestTimeout = timeout ?? TimeSpan.FromSeconds(10);

        services.AddHttpClient(GeocodingClientName, client =>
        {
            client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
            client.Timeout = requestTimeout;
        });
        services.AddHttpClient(ForecastClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com/");
            client.Timeout = requestTimeout;
        });
        services.AddScoped<IWeatherService, OpenMeteoWeatherService>();

        return services;
    }
}
