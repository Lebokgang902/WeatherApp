using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Data;
using WeatherApp.Models;

namespace WeatherApp.Pages.History
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Location Location { get; set; }
        public List<WeatherSnapshot> HistoricalData { get; set; } = new();
        public List<IGrouping<DateTime, WeatherSnapshot>> DailyGroups { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id, int days = 7)
        {
            Location = await _context.Locations
                .FirstOrDefaultAsync(l => l.Id == id);

            if (Location == null)
            {
                return NotFound();
            }

            // Get historical weather data (current weather snapshots, not forecast)
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            HistoricalData = await _context.WeatherSnapshots
                .Where(w => w.LocationId == id
                    && w.ForecastDate == null  // Only current weather snapshots
                    && w.Timestamp >= cutoffDate)
                .OrderByDescending(w => w.Timestamp)
                .ToListAsync();

            // Group by day for chart display
            DailyGroups = HistoricalData
                .GroupBy(w => w.Timestamp.Date)
                .OrderBy(g => g.Key)
                .ToList();

            ViewData["Days"] = days;
            return Page();
        }
    }
}