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

    [Range(1, 20)]
    public int DefaultSets { get; set; } = 3;

    [Range(1, 50)]
    public int DefaultReps { get; set; } = 10;

    [StringLength(300)]
    public string? Notes { get; set; }
}
