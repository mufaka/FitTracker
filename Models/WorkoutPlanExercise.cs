using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

/// <summary>
/// One line of a plan's prescription. The targets are guidance read live during a workout, never
/// copied onto the workout itself (WDM-24, WDM-26) — editing a plan therefore changes what past
/// workouts display as their intent, and can never alter a recorded set.
///
/// A plan is a flat ordered list: duplicates are kept exactly as contributed (WDM-13), because a
/// warm-up's light push-ups and a main block's working push-ups are not the same entry.
/// </summary>
public class WorkoutPlanExercise
{
    public int Id { get; set; }

    [Required]
    public int WorkoutPlanId { get; set; }

    public virtual WorkoutPlan Plan { get; set; } = null!;

    [Required]
    public int ExerciseId { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    /// <summary>Position within the plan, 1-based.</summary>
    public int Order { get; set; }

    // Each target is independently optional, so a plan can prescribe "3 × 10", "45 seconds",
    // "5 km", or nothing at all beyond the movement itself.

    [Range(1, 20)]
    public int? TargetSets { get; set; }

    [Range(1, 50)]
    public int? TargetReps { get; set; }

    [Range(1, 86400)]
    public int? TargetDurationSeconds { get; set; }

    /// <summary>Prescribed distance in canonical kilometres.</summary>
    public decimal? TargetDistance { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}
