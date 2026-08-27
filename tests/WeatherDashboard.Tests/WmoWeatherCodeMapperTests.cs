using WeatherDashboard.Weather;

namespace WeatherDashboard.Tests;

public sealed class WmoWeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, "☀️", "맑음")]
    [InlineData(1, "🌤️", "대체로 맑음")]
    [InlineData(2, "⛅", "부분적으로 흐림")]
    [InlineData(3, "☁️", "흐림")]
    [InlineData(45, "🌫️", "안개")]
    [InlineData(48, "🌫️", "안개")]
    [InlineData(51, "🌦️", "이슬비")]
    [InlineData(53, "🌦️", "이슬비")]
    [InlineData(55, "🌦️", "이슬비")]
    [InlineData(56, "🌧️", "어는 이슬비")]
    [InlineData(57, "🌧️", "어는 이슬비")]
    [InlineData(61, "🌧️", "비")]
    [InlineData(63, "🌧️", "비")]
    [InlineData(65, "🌧️", "비")]
    [InlineData(66, "🌧️", "어는 비")]
    [InlineData(67, "🌧️", "어는 비")]
    [InlineData(71, "🌨️", "눈")]
    [InlineData(73, "🌨️", "눈")]
    [InlineData(75, "🌨️", "눈")]
    [InlineData(77, "🌨️", "싸락눈")]
    [InlineData(80, "🌦️", "소나기")]
    [InlineData(81, "🌦️", "소나기")]
    [InlineData(82, "🌦️", "소나기")]
    [InlineData(85, "🌨️", "눈 소나기")]
    [InlineData(86, "🌨️", "눈 소나기")]
    [InlineData(95, "⛈️", "뇌우")]
    [InlineData(96, "⛈️", "우박을 동반한 뇌우")]
    [InlineData(99, "⛈️", "우박을 동반한 뇌우")]
    [InlineData(999, "❓", "알 수 없음")]
    public void Map_ReturnsExpectedCondition(int code, string emoji, string description)
    {
        var condition = WmoWeatherCodeMapper.Map(code);

        Assert.Equal(code, condition.Code);
        Assert.Equal(emoji, condition.Emoji);
        Assert.Equal(description, condition.Description);
    }
}
