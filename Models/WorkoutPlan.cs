using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

/// <summary>
/// A reusable recipe: the ordered list of exercises a workout is performed from, assembled from any
/// number of templates and individually chosen exercises. Creating a plan does not start training —
/// a <see cref="Workout"/> records what was actually done, and a plan holds no set, weight or
/// performance data of any kind (WDM-19).
///
/// Plans are always user-owned; unlike <see cref="WorkoutTemplate"/> there is no ownerless variant.
/// </summary>
public class WorkoutPlan
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// An inactive plan cannot guide a workout but stays visible so it can be brought back (WDM-16).
    /// Distinct from <see cref="IsDeleted"/>: retiring and deleting behave differently in the list.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Soft delete (WDM-17). Plans are never removed from the database, so the reference a workout
    /// holds stays valid forever and no delete behaviour question arises.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<WorkoutPlanExercise> Exercises { get; set; } = new List<WorkoutPlanExercise>();
}
