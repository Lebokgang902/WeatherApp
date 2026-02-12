using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherApp.Models
{
    public class WeatherSnapshot
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Location")]
        public int? LocationId { get; set; }
        public Location? Location { get; set; }

        [Display(Name = "Time")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Display(Name = "Temperature (°C)")]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal Temperature { get; set; }

        [Display(Name = "Feels Like (°C)")]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal FeelsLike { get; set; }

        [Display(Name = "Humidity (%)")]
        public int Humidity { get; set; }

        [Display(Name = "Pressure (hPa)")]
        public int Pressure { get; set; }

        [Display(Name = "Conditions")]
        public string WeatherMain { get; set; }

        [Display(Name = "Description")]
        public string WeatherDescription { get; set; }

        [Display(Name = "Wind Speed (m/s)")]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal WindSpeed { get; set; }

        [Display(Name = "Forecast Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime? ForecastDate { get; set; }

        // Conflict tracking
        public bool IsConflict { get; set; } = false;
        public string? ConflictDescription { get; set; }  

        // Forecast flag
        public bool IsForecast { get; set; } = false;
    }
}