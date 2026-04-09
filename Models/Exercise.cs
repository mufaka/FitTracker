using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class Exercise
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;
    
    public string MuscleGroups { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string Equipment { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Url]
    public string? VideoUrl { get; set; }
    
    public bool IsCustom { get; set; } = false;
    
    public string? UserId { get; set; }
    
    public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
