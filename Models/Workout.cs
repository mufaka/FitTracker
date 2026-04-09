using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class Workout
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public virtual ApplicationUser User { get; set; } = null!;
    
    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    public int Duration { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsCompleted { get; set; } = false;
    
    public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
