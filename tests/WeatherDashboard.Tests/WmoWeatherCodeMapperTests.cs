using WeatherDashboard.Weather;

namespace WeatherDashboard.Tests;

public sealed class WmoWeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, "☀️", "맑음")]
    [InlineData(3, "☁️", "흐림")]
    [InlineData(65, "🌧️", "비")]
    [InlineData(75, "🌨️", "눈")]
    [InlineData(95, "⛈️", "뇌우")]
    [InlineData(999, "❓", "알 수 없음")]
    public void Map_ReturnsExpectedCondition(int code, string emoji, string description)
    {
        var condition = WmoWeatherCodeMapper.Map(code);

        Assert.Equal(code, condition.Code);
        Assert.Equal(emoji, condition.Emoji);
        Assert.Equal(description, condition.Description);
    }
}
