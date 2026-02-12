using System.Text.Json.Serialization;

namespace WeatherApp.Services
{
    // ============ CURRENT WEATHER RESPONSE ============
    public class CurrentWeatherResponse
    {
        [JsonPropertyName("coord")]
        public Coordinates Coord { get; set; }

        [JsonPropertyName("weather")]
        public WeatherCondition[] Weather { get; set; }

        [JsonPropertyName("main")]
        public MainData Main { get; set; }

        [JsonPropertyName("wind")]
        public WindData Wind { get; set; }

        [JsonPropertyName("sys")]
        public SystemData Sys { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("dt")]
        public long Dt { get; set; }
    }

    // ============ FORECAST RESPONSE ============
    public class ForecastResponse
    {
        [JsonPropertyName("list")]
        public ForecastItem[] List { get; set; }

        [JsonPropertyName("city")]
        public CityInfo City { get; set; }
    }

    public class ForecastItem
    {
        [JsonPropertyName("dt")]
        public long Dt { get; set; }

        [JsonPropertyName("main")]
        public MainData Main { get; set; }

        [JsonPropertyName("weather")]
        public WeatherCondition[] Weather { get; set; }

        [JsonPropertyName("wind")]
        public WindData Wind { get; set; }

        [JsonPropertyName("dt_txt")]
        public string DtTxt { get; set; }
    }

    // ============ SHARED DTOs ============
    public class Coordinates
    {
        [JsonPropertyName("lat")]
        public decimal Lat { get; set; }

        [JsonPropertyName("lon")]
        public decimal Lon { get; set; }
    }

    public class MainData
    {
        [JsonPropertyName("temp")]
        public decimal Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public decimal FeelsLike { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }
    }

    public class WeatherCondition
    {
        [JsonPropertyName("main")]
        public string Main { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }

    public class WindData
    {
        [JsonPropertyName("speed")]
        public decimal Speed { get; set; }
    }

    public class SystemData
    {
        [JsonPropertyName("country")]
        public string Country { get; set; }
    }

    public class CityInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }

    // ============ SERVICE RESULT WRAPPER ============
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public ErrorType? ErrorType { get; set; }
        public int? StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static ServiceResult<T> Fail(string error, ErrorType errorType, int? statusCode = null) =>
            new() { Success = false, ErrorMessage = error, ErrorType = errorType, StatusCode = statusCode };
    }

    public enum ErrorType
    {
        CityNotFound,
        RateLimitExceeded,
        NetworkError,
        ApiError,
        InvalidApiKey,
        Timeout,
        Unknown
    }
}