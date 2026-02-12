using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SyncService _syncService;
        private readonly PreferencesService _preferencesService;  // ✅ ADDED
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            SyncService syncService,
            PreferencesService preferencesService,  // ✅ ADDED
            ILogger<IndexModel> logger)
        {
            _context = context;
            _syncService = syncService;
            _preferencesService = preferencesService;  // ✅ ADDED
            _logger = logger;
        }

        public List<Location> Locations { get; set; } = new();
        public UserPreferences UserPreferences { get; set; }  // ✅ ADDED

        public async Task OnGetAsync()
        {
            // ✅ LOAD USER PREFERENCES
            UserPreferences = await _preferencesService.GetPreferencesAsync();

            Locations = await _context.Locations
                .Include(l => l.WeatherSnapshots)
                .OrderByDescending(l => l.IsFavorite)
                .ThenBy(l => l.DisplayName)
                .ToListAsync();

            // Get most recent weather for each location and convert units
            foreach (var location in Locations)
            {
                var snapshots = location.WeatherSnapshots
                    .Where(w => w.ForecastDate == null)
                    .OrderByDescending(w => w.Timestamp)
                    .Take(1)
                    .ToList();

                // ✅ CONVERT TEMPERATURE AND WIND SPEED BASED ON PREFERENCES
                foreach (var snapshot in snapshots)
                {
                    snapshot.Temperature = await _preferencesService.ConvertTemperatureAsync(snapshot.Temperature);
                    snapshot.FeelsLike = await _preferencesService.ConvertTemperatureAsync(snapshot.FeelsLike);
                    snapshot.WindSpeed = await _preferencesService.ConvertWindSpeedAsync(snapshot.WindSpeed);
                }

                location.WeatherSnapshots = snapshots;
            }
        }

        public async Task<IActionResult> OnPostSyncAsync(int id)
        {
            var result = await _syncService.SyncLocationAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Weather data refreshed successfully!";
                if (result.ConflictDetected)
                {
                    TempData["SyncConflict"] = result.ConflictMessage;
                }
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location != null)
            {
                // SOFT DELETE - Just mark as deleted, don't remove from database
                location.IsDeleted = true;
                location.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Removed {location.DisplayName} from your watchlist.";
            }
            return RedirectToPage();
        }
    }
}