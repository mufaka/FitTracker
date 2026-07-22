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

    /// <summary>
    /// The plan that guided this workout, or <c>null</c> for an ad-hoc one — which stays a
    /// first-class way to train and behaves exactly as workouts always have (WDM-20).
    /// The plan is read live for guidance and is never copied onto the workout.
    /// </summary>
    public int? WorkoutPlanId { get; set; }

    public virtual WorkoutPlan? WorkoutPlan { get; set; }

    public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
    public virtual ICollection<PersonalRecord> PersonalRecords { get; set; } = new List<PersonalRecord>();
}
