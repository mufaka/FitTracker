using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class Achievement
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Icon { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Criteria { get; set; } = string.Empty;

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
