using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class BodyMeasurement
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    public decimal? Weight { get; set; }

    public decimal? BodyFatPercentage { get; set; }

    public decimal? Chest { get; set; }

    public decimal? Waist { get; set; }

    public decimal? Arms { get; set; }

    public decimal? Legs { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
