namespace WeatherDashboard.Weather;

public sealed record WeatherLocation(
    string Name,
    string DisplayName,
    double Latitude,
    double Longitude,
    string Country,
    string? AdministrativeArea,
    string? TimeZone);

public sealed record WeatherCondition(
    int Code,
    string Emoji,
    string Description);

public sealed record CurrentWeather(
    DateTimeOffset Time,
    double TemperatureCelsius,
    double ApparentTemperatureCelsius,
    int RelativeHumidityPercent,
    double WindSpeedMetersPerSecond,
    WeatherCondition Condition);

public sealed record HourlyWeather(
    DateTimeOffset Time,
    double TemperatureCelsius,
    int PrecipitationProbabilityPercent,
    WeatherCondition Condition);

public sealed record DailyWeather(
    DateOnly Date,
    double MaximumTemperatureCelsius,
    double MinimumTemperatureCelsius,
    int PrecipitationProbabilityPercent,
    WeatherCondition Condition);

public sealed record WeatherForecast(
    WeatherLocation Location,
    CurrentWeather Current,
    IReadOnlyList<HourlyWeather> Hourly,
    IReadOnlyList<DailyWeather> Daily);
