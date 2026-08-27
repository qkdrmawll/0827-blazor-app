# 0827-blazor-app
Open-Meteo 기반 .NET 8 Blazor 날씨 대시보드

## 개발

```bash
dotnet build
dotnet test
dotnet run --project src/WeatherDashboard
```

`IWeatherService`는 도시 검색과 현재/시간별/5일 예보 조회 계약을 제공하며,
Open-Meteo 요청의 풍속 단위는 m/s로 고정됩니다.
