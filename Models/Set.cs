using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class Set
{
    public int Id { get; set; }
    
    [Required]
    public int WorkoutExerciseId { get; set; }
    
    public virtual WorkoutExercise WorkoutExercise { get; set; } = null!;
    
    public int SetNumber { get; set; }
    
    public int? Reps { get; set; }
    
    /// <summary>Canonical kilograms; converted from the user's unit on the way in.</summary>
    public decimal? Weight { get; set; }

    /// <summary>Seconds. Unit-independent, so never converted (WDM-37).</summary>
    public int? Duration { get; set; }

    /// <summary>Canonical kilometres; converted from the user's unit on the way in.</summary>
    public decimal? Distance { get; set; }

    public int? RestTime { get; set; }
    
    public int? RPE { get; set; }

    /// <summary>
    /// Whether <see cref="RPE"/> was implied by the exercise's effort rating rather than typed. Only a
    /// derived value is ever overwritten or cleared when that rating changes — what the user entered
    /// is theirs and stands, which is the whole reason this is recorded rather than inferred later.
    /// </summary>
    public bool IsRpeDerived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
