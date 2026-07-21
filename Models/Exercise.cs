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

    /// <summary>
    /// Whether this exercise is loaded with an external weight, and so has a meaningful one-rep max.
    /// Running, planks and bodyweight movements do not; barbell, dumbbell, cable and machine work does.
    /// Seeded from category and equipment, then editable per exercise — no heuristic stays right forever.
    /// </summary>
    public bool TracksOneRepMax { get; set; } = false;

    public string? UserId { get; set; }
    
    public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
    public virtual ICollection<WorkoutTemplateExercise> WorkoutTemplateExercises { get; set; } = new List<WorkoutTemplateExercise>();
    public virtual ICollection<WorkoutPlanExercise> WorkoutPlanExercises { get; set; } = new List<WorkoutPlanExercise>();
    public virtual ICollection<PersonalRecord> PersonalRecords { get; set; } = new List<PersonalRecord>();
}
