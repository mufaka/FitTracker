using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

/// <summary>
/// A user's participation in a challenge. Progress is not stored: it is derived
/// from workouts inside the window whenever it is needed, the same way
/// achievement progress is. Only joining and completing are events worth keeping.
/// </summary>
public class UserChallenge
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    public int ChallengeId { get; set; }

    public virtual Challenge Challenge { get; set; } = null!;

    /// <summary>Start of the window. The end is this plus the challenge duration.</summary>
    public DateTime StartedDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>Set once the goal is reached; null while still in progress.</summary>
    public DateTime? CompletedDate { get; set; }
}
