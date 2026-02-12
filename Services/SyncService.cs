using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class SyncService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeatherService _weatherService;
        private readonly ILogger<SyncService> _logger;

        public SyncService(
            ApplicationDbContext context,
            IWeatherService weatherService,
            ILogger<SyncService> logger)
        {
            _context = context;
            _weatherService = weatherService;
            _logger = logger;
        }

        public async Task<SyncResult> SyncLocationAsync(int locationId)
        {
            _logger.LogInformation($"=== STARTING SYNC for Location ID: {locationId} ===");

            var location = await _context.Locations.FindAsync(locationId);
            if (location == null)
            {
                _logger.LogWarning($"Location {locationId} not found");
                return SyncResult.Fail("Location not found");
            }

            _logger.LogInformation($"Location found: {location.City}, {location.Country}");

            try
            {
                var syncResult = new SyncResult { LocationId = locationId };

                // 1. Fetch current weather
                _logger.LogInformation($"Step 1: Fetching current weather for {location.City}, {location.Country}");
                var currentResult = await _weatherService.GetCurrentWeatherAsync(location.City, location.Country);

                if (!currentResult.Success)
                {
                    _logger.LogError($"Step 1 FAILED: {currentResult.ErrorMessage}");
                    return SyncResult.Fail(currentResult.ErrorMessage, currentResult.ErrorType);
                }

                _logger.LogInformation($"Step 1 SUCCESS: Temperature = {currentResult.Data.Main.Temp}°C");

                // 2. Check for significant temperature differences
                _logger.LogInformation("Step 2: Checking for temperature conflicts");
                var lastSnapshot = await _context.WeatherSnapshots
                    .Where(w => w.LocationId == locationId && w.ForecastDate == null)
                    .OrderByDescending(w => w.Timestamp)
                    .FirstOrDefaultAsync();

                if (lastSnapshot != null)
                {
                    var tempDiff = Math.Abs(currentResult.Data.Main.Temp - lastSnapshot.Temperature);
                    _logger.LogInformation($"Previous temp: {lastSnapshot.Temperature}°C, Current temp: {currentResult.Data.Main.Temp}°C, Difference: {tempDiff}°C");

                    if (tempDiff > 10)
                    {
                        _logger.LogWarning($"Conflict detected! Temperature difference of {tempDiff}°C");
                        syncResult.ConflictDetected = true;
                        syncResult.ConflictMessage = $"Temperature changed from {lastSnapshot.Temperature:F1}°C to {currentResult.Data.Main.Temp:F1}°C";
                    }
                }

                // 3. Store current weather
                _logger.LogInformation("Step 3: Saving current weather snapshot");
                try
                {
                    var snapshot = new WeatherSnapshot
                    {
                        LocationId = location.Id,
                        Temperature = currentResult.Data.Main.Temp,
                        FeelsLike = currentResult.Data.Main.FeelsLike,
                        Humidity = currentResult.Data.Main.Humidity,
                        Pressure = currentResult.Data.Main.Pressure,
                        WeatherMain = currentResult.Data.Weather[0]?.Main ?? "Unknown",
                        WeatherDescription = currentResult.Data.Weather[0]?.Description ?? "Unknown",
                        WindSpeed = currentResult.Data.Wind.Speed,
                        ForecastDate = null,
                        IsConflict = syncResult.ConflictDetected,
                        ConflictDescription = syncResult.ConflictMessage,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.WeatherSnapshots.Add(snapshot);
                    syncResult.CurrentWeatherAdded = true;
                    _logger.LogInformation("Snapshot created successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Step 3 FAILED: Error creating weather snapshot");
                    throw;
                }

                // 4. Fetch 5-day forecast
                _logger.LogInformation($"Step 4: Fetching 5-day forecast for {location.City}, {location.Country}");
                try
                {
                    var forecastResult = await _weatherService.GetForecastAsync(location.City, location.Country);

                    if (forecastResult.Success)
                    {
                        _logger.LogInformation($"Forecast API returned {forecastResult.Data.List?.Length ?? 0} items");

                        var forecastAdded = 0;
                        if (forecastResult.Data.List != null)
                        {
                            foreach (var item in forecastResult.Data.List)
                            {
                                var forecastDateTime = DateTimeOffset.FromUnixTimeSeconds(item.Dt).DateTime;

                                // Check if we already have this forecast
                                var exists = await _context.WeatherSnapshots
                                    .AnyAsync(w => w.LocationId == location.Id &&
                                                  w.ForecastDate == forecastDateTime);

                                if (!exists)
                                {
                                    _context.WeatherSnapshots.Add(new WeatherSnapshot
                                    {
                                        LocationId = location.Id,
                                        Temperature = item.Main.Temp,
                                        WeatherMain = item.Weather[0]?.Main ?? "Unknown",
                                        WeatherDescription = item.Weather[0]?.Description ?? "Unknown",
                                        ForecastDate = forecastDateTime,
                                        IsForecast = true
                                    });
                                    forecastAdded++;
                                }
                            }
                        }

                        syncResult.ForecastItemsAdded = forecastAdded;
                        _logger.LogInformation($"Added {forecastAdded} new forecast items");
                    }
                    else
                    {
                        _logger.LogWarning($"Forecast API failed: {forecastResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Step 4 FAILED: Error fetching forecast");
                    // Don't throw - we still want to save current weather even if forecast fails
                }

                // 5. Update location sync timestamp
                _logger.LogInformation("Step 5: Updating last sync timestamp");
                location.LastSync = DateTime.UtcNow;

                _logger.LogInformation("Step 6: Saving all changes to database");
                await _context.SaveChangesAsync();

                syncResult.Success = true;
                _logger.LogInformation($"=== SYNC COMPLETED SUCCESSFULLY for Location ID: {locationId} ===");

                return syncResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SYNC FAILED for Location ID: {locationId} ===", locationId);
                _logger.LogError($"Exception Type: {ex.GetType().Name}");
                _logger.LogError($"Exception Message: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                }

                return SyncResult.Fail($"Sync failed: {ex.Message}");
            }
        }
        public async Task<BulkSyncResult> SyncAllLocationsAsync()
        {
            var locations = await _context.Locations.ToListAsync();
            var results = new List<SyncResult>();

            foreach (var location in locations)
            {
                var result = await SyncLocationAsync(location.Id);
                results.Add(result);

                // Small delay to avoid rate limiting
                await Task.Delay(100);
            }

            return new BulkSyncResult
            {
                TotalLocations = locations.Count,
                Successful = results.Count(r => r.Success),
                Failed = results.Count(r => !r.Success),
                Results = results
            };
        }
    }

    // Result classes - add these at the bottom of the same file
    public class SyncResult
    {
        public bool Success { get; set; }
        public int LocationId { get; set; }
        public string ErrorMessage { get; set; }
        public ErrorType? ErrorType { get; set; }
        public bool ConflictDetected { get; set; }
        public string ConflictMessage { get; set; }
        public bool CurrentWeatherAdded { get; set; }
        public int ForecastItemsAdded { get; set; }

        public static SyncResult Fail(string error, ErrorType? errorType = null)
        {
            return new SyncResult
            {
                Success = false,
                ErrorMessage = error,
                ErrorType = errorType
            };
        }
    }

    public class BulkSyncResult
    {
        public int TotalLocations { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<SyncResult> Results { get; set; } = new();
    }
}