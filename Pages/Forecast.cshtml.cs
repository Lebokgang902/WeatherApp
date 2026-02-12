using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Pages
{
    public class ForecastModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ForecastModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Location Location { get; set; }
        public List<WeatherSnapshot> Forecasts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Location = await _context.Locations
                .FirstOrDefaultAsync(l => l.Id == id);

            if (Location == null)
            {
                return NotFound();
            }

            // Get 5-day forecast (grouped by day)
            Forecasts = await _context.WeatherSnapshots
                .Where(w => w.LocationId == id && w.ForecastDate != null)
                .OrderBy(w => w.ForecastDate)
                .Take(40) // 5 days × 8 readings per day
                .ToListAsync();

            return Page();
        }
    }
}