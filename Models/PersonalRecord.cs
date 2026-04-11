using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class PersonalRecord
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    public int ExerciseId { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    [Required]
    public int WorkoutId { get; set; }

    public virtual Workout Workout { get; set; } = null!;

    public decimal Weight { get; set; }

    public int Reps { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public decimal OneRepMax { get; set; }
}
