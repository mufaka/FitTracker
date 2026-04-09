using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class WorkoutExercise
{
    public int Id { get; set; }
    
    [Required]
    public int WorkoutId { get; set; }
    
    public virtual Workout Workout { get; set; } = null!;
    
    [Required]
    public int ExerciseId { get; set; }
    
    public virtual Exercise Exercise { get; set; } = null!;
    
    public int Order { get; set; }
    
    public string? Notes { get; set; }
    
    public virtual ICollection<Set> Sets { get; set; } = new List<Set>();
}
