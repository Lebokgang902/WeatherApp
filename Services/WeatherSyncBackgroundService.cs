using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;

namespace WeatherApp.Services
{
    public class WeatherSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<WeatherSyncBackgroundService> _logger;

        public WeatherSyncBackgroundService(
            IServiceProvider services,
            ILogger<WeatherSyncBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Weather Auto-Sync Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
                        var prefsService = scope.ServiceProvider.GetRequiredService<PreferencesService>();

                        // Get user preferences for refresh interval
                        var prefs = await prefsService.GetPreferencesAsync();

                        if (prefs.AutoRefreshEnabled)
                        {
                            var locations = await context.Locations
                                .Where(l => !l.IsDeleted)
                                .ToListAsync(stoppingToken);

                            _logger.LogInformation($"Auto-syncing {locations.Count} locations");

                            foreach (var location in locations)
                            {
                                await syncService.SyncLocationAsync(location.Id);
                                await Task.Delay(1000, stoppingToken); // Rate limiting
                            }
                        }
                    }

                    // Wait for refresh interval (default 30 minutes)
                    using (var scope = _services.CreateScope())
                    {
                        var prefsService = scope.ServiceProvider.GetRequiredService<PreferencesService>();
                        var prefs = await prefsService.GetPreferencesAsync();
                        var interval = prefs.RefreshInterval * 60 * 1000; // Convert to milliseconds

                        await Task.Delay(interval, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in weather auto-sync service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}