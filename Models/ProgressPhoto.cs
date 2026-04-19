using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class ProgressPhoto
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [StringLength(260)]
    public string PhotoPath { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContentType { get; set; } = "image/jpeg";

    [StringLength(500)]
    public string? Notes { get; set; }
}
