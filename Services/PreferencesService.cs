using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class PreferencesService
    {
        private readonly ApplicationDbContext _context;
        private UserPreferences? _cachedPreferences;
        private DateTime _cacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);

        public PreferencesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserPreferences> GetPreferencesAsync()
        {
            // Return cached preferences if still valid
            if (_cachedPreferences != null && DateTime.UtcNow - _cacheTime < _cacheDuration)
            {
                return _cachedPreferences;
            }

            // Get from database or create default
            _cachedPreferences = await _context.UserPreferences.FirstOrDefaultAsync();
            if (_cachedPreferences == null)
            {
                _cachedPreferences = new UserPreferences();
                _context.UserPreferences.Add(_cachedPreferences);
                await _context.SaveChangesAsync();
            }

            _cacheTime = DateTime.UtcNow;
            return _cachedPreferences;
        }

        // Convert temperature based on user preference
        public async Task<decimal> ConvertTemperatureAsync(decimal celsius)
        {
            var prefs = await GetPreferencesAsync();

            if (prefs.TemperatureUnit == "Fahrenheit")
            {
                return (celsius * 9 / 5) + 32; // Convert to Fahrenheit
            }

            return celsius; // Default Celsius
        }

        // Get temperature unit symbol
        public async Task<string> GetTemperatureUnitSymbolAsync()
        {
            var prefs = await GetPreferencesAsync();
            return prefs.TemperatureUnit == "Fahrenheit" ? "°F" : "°C";
        }

        // Format wind speed based on user preference
        public async Task<decimal> ConvertWindSpeedAsync(decimal ms)
        {
            var prefs = await GetPreferencesAsync();

            return prefs.WindSpeedUnit switch
            {
                "kmh" => ms * 3.6m,      // m/s to km/h
                "mph" => ms * 2.237m,     // m/s to mph
                _ => ms                   // Default m/s
            };
        }

        // Get wind speed unit symbol
        public async Task<string> GetWindSpeedUnitSymbolAsync()
        {
            var prefs = await GetPreferencesAsync();

            return prefs.WindSpeedUnit switch
            {
                "kmh" => "km/h",
                "mph" => "mph",
                _ => "m/s"
            };
        }

        // Check if coordinates should be shown
        public async Task<bool> ShowCoordinatesAsync()
        {
            var prefs = await GetPreferencesAsync();
            return prefs.ShowCoordinates;
        }

        // Get refresh interval in minutes
        public async Task<int> GetRefreshIntervalAsync()
        {
            var prefs = await GetPreferencesAsync();
            return prefs.RefreshInterval;
        }

        // Get theme
        public async Task<string> GetThemeAsync()
        {
            var prefs = await GetPreferencesAsync();
            return prefs.Theme;
        }
    }
}