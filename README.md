# 0827-blazor-app

Open-Meteo 기반 .NET 8 Blazor 날씨 대시보드

## 요구 사항

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## 실행

```bash
dotnet restore
dotnet run --project src/WeatherDashboard/WeatherDashboard.csproj
```

터미널에 표시된 로컬 URL을 브라우저에서 엽니다.

## 빌드 및 테스트

```bash
dotnet build WeatherDashboard.sln
dotnet test WeatherDashboard.sln --no-build
```

테스트는 고정된 HTTP 응답을 사용하므로 외부 네트워크에 의존하지 않습니다.

## 날씨 데이터

도시 검색에는 [Open-Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api)를,
현재/시간별/5일 예보에는 [Open-Meteo Forecast API](https://open-meteo.com/en/docs)를 사용합니다.
비상업적 이용에는 API 키가 필요하지 않습니다. 풍속 요청 단위는 m/s로 고정됩니다.
