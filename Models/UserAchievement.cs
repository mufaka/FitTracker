using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class UserAchievement
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    public int AchievementId { get; set; }

    public virtual Achievement Achievement { get; set; } = null!;

    public DateTime UnlockedDate { get; set; } = DateTime.UtcNow;
}
