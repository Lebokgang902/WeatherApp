using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public UserPreferences Preferences { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get existing preferences or create default
            Preferences = await _context.UserPreferences.FirstOrDefaultAsync();

            if (Preferences == null)
            {
                Preferences = new UserPreferences();
                // Don't save here - wait until user clicks Save
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Preferences.LastUpdated = DateTime.UtcNow;

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
            TempData["SuccessMessage"] = "Preferences saved successfully!";

            return RedirectToPage();
        }
    }
}