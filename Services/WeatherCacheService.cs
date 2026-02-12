using Microsoft.Extensions.Caching.Memory;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class WeatherCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeatherCacheService> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5); // Cache for 5 minutes

        public WeatherCacheService(IMemoryCache cache, ILogger<WeatherCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        // Get cached current weather or fetch new
        public async Task<ServiceResult<CurrentWeatherResponse>> GetOrFetchCurrentWeatherAsync(
            string city,
            string country,
            Func<Task<ServiceResult<CurrentWeatherResponse>>> fetchFunction)
        {
            var cacheKey = $"current_{city}_{country}".ToLower();

            if (_cache.TryGetValue(cacheKey, out ServiceResult<CurrentWeatherResponse> cachedResult))
            {
                _logger.LogInformation($"Cache HIT for {city}, {country}");
                return cachedResult;
            }

            _logger.LogInformation($"Cache MISS for {city}, {country}");
            var result = await fetchFunction();

            if (result.Success)
            {
                _cache.Set(cacheKey, result, _cacheDuration);
            }

            return result;
        }

        // Get cached forecast or fetch new
        public async Task<ServiceResult<ForecastResponse>> GetOrFetchForecastAsync(
            string city,
            string country,
            Func<Task<ServiceResult<ForecastResponse>>> fetchFunction)
        {
            var cacheKey = $"forecast_{city}_{country}".ToLower();

            if (_cache.TryGetValue(cacheKey, out ServiceResult<ForecastResponse> cachedResult))
            {
                _logger.LogInformation($"Cache HIT for forecast {city}, {country}");
                return cachedResult;
            }

            _logger.LogInformation($"Cache MISS for forecast {city}, {country}");
            var result = await fetchFunction();

            if (result.Success)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromHours(1)); // Forecast cached for 1 hour
            }

            return result;
        }

        // Clear cache for a location (when forcing refresh)
        public void ClearLocationCache(string city, string country)
        {
            var currentKey = $"current_{city}_{country}".ToLower();
            var forecastKey = $"forecast_{city}_{country}".ToLower();

            _cache.Remove(currentKey);
            _cache.Remove(forecastKey);

            _logger.LogInformation($"Cache cleared for {city}, {country}");
        }
    }
}