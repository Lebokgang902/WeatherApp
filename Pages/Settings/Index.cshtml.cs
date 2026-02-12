using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;
using WeatherApp.Services;  // ✅ ADD THIS

namespace WeatherApp.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly PreferencesService _preferencesService;  // ✅ ADD THIS

        public IndexModel(
            ApplicationDbContext context,
            PreferencesService preferencesService)  // ✅ ADD THIS
        {
            _context = context;
            _preferencesService = preferencesService;  // ✅ ADD THIS
        }

        [BindProperty]
        public UserPreferences Preferences { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get existing preferences using the service
            Preferences = await _preferencesService.GetPreferencesAsync();  // ✅ UPDATED

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Preferences.LastUpdated = DateTime.UtcNow;

            try
            {
                // Check if preferences already exist
                var existingPrefs = await _context.UserPreferences.FirstOrDefaultAsync();

                if (existingPrefs != null)
                {
                    // Update existing record - copy all values
                    existingPrefs.TemperatureUnit = Preferences.TemperatureUnit;
                    existingPrefs.WindSpeedUnit = Preferences.WindSpeedUnit;
                    existingPrefs.RefreshInterval = Preferences.RefreshInterval;
                    existingPrefs.AutoRefreshEnabled = Preferences.AutoRefreshEnabled;
                    existingPrefs.Theme = Preferences.Theme;
                    existingPrefs.ShowCoordinates = Preferences.ShowCoordinates;
                    existingPrefs.DefaultCity = Preferences.DefaultCity;
                    existingPrefs.DefaultCountry = Preferences.DefaultCountry;
                    existingPrefs.LastUpdated = DateTime.UtcNow;

                    _context.UserPreferences.Update(existingPrefs);
                }
                else
                {
                    // Add new record
                    _context.UserPreferences.Add(Preferences);
                }

                await _context.SaveChangesAsync();

                // Clear the cache in PreferencesService so it picks up new values
                await _preferencesService.GetPreferencesAsync();  // ✅ REFRESH CACHE

                TempData["SuccessMessage"] = "Preferences saved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving preferences: {ex.Message}";
                return Page();
            }

            return RedirectToPage();
        }
    }
}