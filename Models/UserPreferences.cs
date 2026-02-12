using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherApp.Models
{
    public class UserPreferences
    {
     
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Temperature Unit")]
        public string TemperatureUnit { get; set; } = "Celsius"; // Celsius or Fahrenheit

        [Display(Name = "Wind Speed Unit")]
        public string WindSpeedUnit { get; set; } = "ms"; // ms, kmh, mph

        [Display(Name = "Refresh Interval (minutes)")]
        [Range(5, 120, ErrorMessage = "Refresh interval must be between 5 and 120 minutes.")]
        public int RefreshInterval { get; set; } = 30;

        [Display(Name = "Auto Refresh")]
        public bool AutoRefreshEnabled { get; set; } = true;

        [Display(Name = "Default City")]
        public string? DefaultCity { get; set; }

        [Display(Name = "Default Country")]
        [StringLength(2, MinimumLength = 2)]
        public string? DefaultCountry { get; set; }

        [Display(Name = "Theme")]
        public string Theme { get; set; } = "Light"; // Light, Dark, Auto

        [Display(Name = "Show Coordinates")]
        public bool ShowCoordinates { get; set; } = true;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}