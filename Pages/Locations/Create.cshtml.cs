using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Pages.Locations
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeatherService _weatherService;
        private readonly SyncService _syncService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context,
            IWeatherService weatherService,
            SyncService syncService,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _weatherService = weatherService;
            _syncService = syncService;
            _logger = logger;
        }

        [BindProperty]
        public string City { get; set; }

        [BindProperty]
        public string Country { get; set; }

        [BindProperty]
        public string DisplayName { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(City) || string.IsNullOrWhiteSpace(Country))
            {
                ErrorMessage = "City and Country are required";
                return Page();
            }

            // Validate city by fetching weather
            var weatherResult = await _weatherService.GetCurrentWeatherAsync(City, Country);

            if (!weatherResult.Success)
            {
                ErrorMessage = weatherResult.ErrorMessage;
                return Page();
            }

            // Check if location already exists
            var existingLocation = await _context.Locations
                .FirstOrDefaultAsync(l => l.City.ToLower() == City.ToLower() &&
                                         l.Country.ToLower() == Country.ToLower());

            if (existingLocation != null)
            {
                ErrorMessage = $"This city ({City}, {Country}) is already being tracked.";
                return Page();
            }

            // Create new location
            var location = new Location
            {
                City = City,
                Country = Country.ToUpper(),
                DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? City : DisplayName,
                Latitude = weatherResult.Data.Coord.Lat,
                Longitude = weatherResult.Data.Coord.Lon,
                CreatedAt = DateTime.UtcNow,
                IsFavorite = false
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            // Initial sync
            await _syncService.SyncLocationAsync(location.Id);

            TempData["SuccessMessage"] = $"Added {location.DisplayName} to your weather dashboard!";
            return RedirectToPage("/Index");
        }
    }
}