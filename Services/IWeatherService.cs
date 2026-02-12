namespace WeatherApp.Services
{
    public interface IWeatherService
    {
        // Core API methods
        Task<ServiceResult<CurrentWeatherResponse>> GetCurrentWeatherAsync(string city, string country);
        Task<ServiceResult<ForecastResponse>> GetForecastAsync(string city, string country);

        // Utility methods
        Task<ServiceResult<bool>> ValidateCityAsync(string city, string country);
        Task<ServiceResult<Coordinates>> GetCoordinatesAsync(string city, string country);
    }
}