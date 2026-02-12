using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Caching.Memory;

namespace WeatherApp.Services
{
    public class OpenWeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<OpenWeatherService> _logger;
        private readonly IConfiguration _configuration;
        private readonly WeatherCacheService _cacheService; // ✅ ADDED

        // Rate limiting tracking
        private static int _apiCallsThisMinute = 0;
        private static DateTime _rateLimitResetTime = DateTime.UtcNow.AddMinutes(1);
        private const int MAX_CALLS_PER_MINUTE = 60;

        public OpenWeatherService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenWeatherService> logger,
            WeatherCacheService cacheService) // ✅ ADDED
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _cacheService = cacheService; // ✅ ADDED

            _apiKey = configuration["OpenWeatherApi:ApiKey"]
                ?? throw new InvalidOperationException("OpenWeather API key not configured");

            _httpClient.BaseAddress = new Uri(configuration["OpenWeatherApi:BaseUrl"]
                ?? "https://api.openweathermap.org/data/2.5/");

            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WeatherApp/1.0");
        }

        public async Task<ServiceResult<CurrentWeatherResponse>> GetCurrentWeatherAsync(string city, string country)
        {
            // ✅ USE CACHE - Try to get from cache first
            return await _cacheService.GetOrFetchCurrentWeatherAsync(city, country, async () =>
            {
                try
                {
                    // Check rate limit
                    if (!CheckRateLimit())
                    {
                        return ServiceResult<CurrentWeatherResponse>.Fail(
                            "Rate limit exceeded. Please try again later.",
                            ErrorType.RateLimitExceeded,
                            429);
                    }

                    var encodedCity = HttpUtility.UrlEncode(city);
                    var encodedCountry = HttpUtility.UrlEncode(country);
                    var url = $"weather?q={encodedCity},{encodedCountry}&units=metric&appid={_apiKey}";

                    _logger.LogInformation("Calling OpenWeather API: {Url}", url.Replace(_apiKey, "HIDDEN"));

                    var response = await _httpClient.GetAsync(url);

                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.OK:
                            var content = await response.Content.ReadAsStringAsync();
                            var weather = JsonSerializer.Deserialize<CurrentWeatherResponse>(content, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (weather == null)
                            {
                                return ServiceResult<CurrentWeatherResponse>.Fail(
                                    "Invalid response format from weather service",
                                    ErrorType.ApiError,
                                    (int)response.StatusCode);
                            }

                            return ServiceResult<CurrentWeatherResponse>.Ok(weather);

                        case System.Net.HttpStatusCode.NotFound:
                            return ServiceResult<CurrentWeatherResponse>.Fail(
                                $"City '{city}, {country}' not found",
                                ErrorType.CityNotFound,
                                404);

                        case System.Net.HttpStatusCode.Unauthorized:
                            return ServiceResult<CurrentWeatherResponse>.Fail(
                                "Invalid API key. Please check your OpenWeatherMap API key.",
                                ErrorType.InvalidApiKey,
                                401);

                        case System.Net.HttpStatusCode.TooManyRequests:
                            return ServiceResult<CurrentWeatherResponse>.Fail(
                                "Rate limit exceeded. Please try again later.",
                                ErrorType.RateLimitExceeded,
                                429);

                        default:
                            return ServiceResult<CurrentWeatherResponse>.Fail(
                                $"Weather API error: {response.StatusCode}",
                                ErrorType.ApiError,
                                (int)response.StatusCode);
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogError(ex, "Timeout calling weather API for {city}", city);
                    return ServiceResult<CurrentWeatherResponse>.Fail(
                        "Request timeout. The weather service is taking too long to respond.",
                        ErrorType.Timeout);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Network error calling weather API for {city}", city);
                    return ServiceResult<CurrentWeatherResponse>.Fail(
                        $"Network error: {ex.Message}",
                        ErrorType.NetworkError);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON parsing error for {city}", city);
                    return ServiceResult<CurrentWeatherResponse>.Fail(
                        "Invalid data format received from weather service",
                        ErrorType.ApiError);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error calling weather API for {city}", city);
                    return ServiceResult<CurrentWeatherResponse>.Fail(
                        "An unexpected error occurred",
                        ErrorType.Unknown);
                }
            });
        }

        public async Task<ServiceResult<ForecastResponse>> GetForecastAsync(string city, string country)
        {
            // ✅ USE CACHE - Try to get from cache first
            return await _cacheService.GetOrFetchForecastAsync(city, country, async () =>
            {
                try
                {
                    if (!CheckRateLimit())
                    {
                        return ServiceResult<ForecastResponse>.Fail(
                            "Rate limit exceeded. Please try again later.",
                            ErrorType.RateLimitExceeded,
                            429);
                    }

                    var encodedCity = HttpUtility.UrlEncode(city);
                    var encodedCountry = HttpUtility.UrlEncode(country);
                    var url = $"forecast?q={encodedCity},{encodedCountry}&units=metric&appid={_apiKey}&cnt=40";

                    var response = await _httpClient.GetAsync(url);

                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.OK:
                            var content = await response.Content.ReadAsStringAsync();
                            var forecast = JsonSerializer.Deserialize<ForecastResponse>(content, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (forecast?.List == null)
                            {
                                return ServiceResult<ForecastResponse>.Fail(
                                    "Invalid forecast data format",
                                    ErrorType.ApiError,
                                    (int)response.StatusCode);
                            }

                            return ServiceResult<ForecastResponse>.Ok(forecast);

                        case System.Net.HttpStatusCode.NotFound:
                            return ServiceResult<ForecastResponse>.Fail(
                                $"City '{city}, {country}' not found",
                                ErrorType.CityNotFound,
                                404);

                        default:
                            return ServiceResult<ForecastResponse>.Fail(
                                $"Forecast API error: {response.StatusCode}",
                                ErrorType.ApiError,
                                (int)response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching forecast for {city}", city);
                    return ServiceResult<ForecastResponse>.Fail(
                        ex.Message,
                        ErrorType.Unknown);
                }
            });
        }

        public async Task<ServiceResult<bool>> ValidateCityAsync(string city, string country)
        {
            var result = await GetCurrentWeatherAsync(city, country);
            if (result.Success)
            {
                return ServiceResult<bool>.Ok(true);
            }

            return ServiceResult<bool>.Fail(result.ErrorMessage, result.ErrorType.Value, result.StatusCode);
        }

        public async Task<ServiceResult<Coordinates>> GetCoordinatesAsync(string city, string country)
        {
            var result = await GetCurrentWeatherAsync(city, country);
            if (result.Success && result.Data?.Coord != null)
            {
                return ServiceResult<Coordinates>.Ok(result.Data.Coord);
            }

            return ServiceResult<Coordinates>.Fail(
                result.ErrorMessage ?? "Could not get coordinates",
                result.ErrorType ?? ErrorType.Unknown,
                result.StatusCode);
        }

        // ✅ NEW METHOD: Clear cache when manually refreshing
        public void ClearCache(string city, string country)
        {
            _cacheService.ClearLocationCache(city, country);
            _logger.LogInformation($"Cache cleared for {city}, {country}");
        }

        private bool CheckRateLimit()
        {
            // Reset counter if minute has passed
            if (DateTime.UtcNow > _rateLimitResetTime)
            {
                _apiCallsThisMinute = 0;
                _rateLimitResetTime = DateTime.UtcNow.AddMinutes(1);
            }

            if (_apiCallsThisMinute >= MAX_CALLS_PER_MINUTE)
            {
                _logger.LogWarning("Rate limit exceeded. Calls this minute: {Calls}", _apiCallsThisMinute);
                return false;
            }

            _apiCallsThisMinute++;
            return true;
        }
    }
}