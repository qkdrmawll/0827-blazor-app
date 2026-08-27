namespace WeatherDashboard.Weather;

public static class WmoWeatherCodeMapper
{
    public static WeatherCondition Map(int code) => code switch
    {
        0 => new(code, "☀️", "맑음"),
        1 => new(code, "🌤️", "대체로 맑음"),
        2 => new(code, "⛅", "부분적으로 흐림"),
        3 => new(code, "☁️", "흐림"),
        45 or 48 => new(code, "🌫️", "안개"),
        51 or 53 or 55 => new(code, "🌦️", "이슬비"),
        56 or 57 => new(code, "🌧️", "어는 이슬비"),
        61 or 63 or 65 => new(code, "🌧️", "비"),
        66 or 67 => new(code, "🌧️", "어는 비"),
        71 or 73 or 75 => new(code, "🌨️", "눈"),
        77 => new(code, "🌨️", "싸락눈"),
        80 or 81 or 82 => new(code, "🌦️", "소나기"),
        85 or 86 => new(code, "🌨️", "눈 소나기"),
        95 => new(code, "⛈️", "뇌우"),
        96 or 99 => new(code, "⛈️", "우박을 동반한 뇌우"),
        _ => new(code, "❓", "알 수 없음")
    };
}
