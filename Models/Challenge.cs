using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

/// <summary>
/// A challenge definition. Deliberately evergreen: the window is measured from
/// the date each user joins rather than fixed on the definition, so a seeded
/// challenge never ships already expired.
/// </summary>
public class Challenge
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

    /// <summary>
    /// What is being measured. Shares its vocabulary with
    /// <see cref="FitTracker.Services.AchievementCriteria"/>.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string GoalType { get; set; } = string.Empty;

    /// <summary>The value that has to be reached within the window.</summary>
    public decimal Goal { get; set; }

    /// <summary>Length of the window, in days, starting the day the user joins.</summary>
    public int DurationDays { get; set; }

    public virtual ICollection<UserChallenge> UserChallenges { get; set; } = new List<UserChallenge>();
}
