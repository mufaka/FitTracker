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
    
    public decimal? Weight { get; set; }
    
    public int? Duration { get; set; }
    
    public int? RestTime { get; set; }
    
    public int? RPE { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
