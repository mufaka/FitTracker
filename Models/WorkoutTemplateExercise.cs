using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class WorkoutTemplateExercise
{
    public int Id { get; set; }

    [Required]
    public int TemplateId { get; set; }

    public virtual WorkoutTemplate Template { get; set; } = null!;

    [Required]
    public int ExerciseId { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }

    // A template is a reusable grouping, not necessarily a full session, so every part of the
    // prescription is independently optional (WDM-03). A stretch prescribes a duration and no reps;
    // a run prescribes a distance and neither.

    [Range(1, 20)]
    public int? DefaultSets { get; set; }

    [Range(1, 50)]
    public int? DefaultReps { get; set; }

    [Range(1, 86400)]
    public int? DefaultDurationSeconds { get; set; }

    /// <summary>Prescribed distance in canonical kilometres.</summary>
    public decimal? DefaultDistance { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}
