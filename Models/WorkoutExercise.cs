using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

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

    /// <summary>
    /// One of <see cref="WorkoutExerciseStatuses"/>. Rows now exist before the user has touched them
    /// — starting a workout from a plan materializes one per planned exercise — so the status is what
    /// separates "not addressed yet" from "deliberately skipped" from "done, and here is how it felt".
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = WorkoutExerciseStatuses.Pending;

    /// <summary>
    /// Whether this counts as trained, for any derived statistic (WDM-54). The presence of the row
    /// itself proves nothing once rows are created up front: an exercise counts when it has at least
    /// one recorded set, or when the user rated the effort.
    /// </summary>
    public bool IsPerformed => Sets.Count > 0 || WorkoutExerciseStatuses.IsEffort(Status);

    public virtual ICollection<Set> Sets { get; set; } = new List<Set>();
}

/// <summary>
/// The closed set of values <see cref="WorkoutExercise.Status"/> may take. Strings against a
/// constants class rather than a CLR enum, following <c>AchievementCriteria</c> and
/// <c>ChallengeGoalTypes</c> — the codebase contains no enums.
/// </summary>
public static class WorkoutExerciseStatuses
{
    /// <summary>Not yet addressed; the default on creation.</summary>
    public const string Pending = "Pending";

    /// <summary>Deliberately not performed.</summary>
    public const string Skipped = "Skipped";

    public const string Easy = "Easy";
    public const string Medium = "Medium";
    public const string Hard = "Hard";

    /// <summary>The statuses that mean the exercise was performed, and record how it felt.</summary>
    public static readonly IReadOnlyList<string> Effort = new[] { Easy, Medium, Hard };

    public static readonly IReadOnlyList<string> All = new[] { Pending, Skipped, Easy, Medium, Hard };

    public static bool IsEffort(string? status) =>
        status == Easy || status == Medium || status == Hard;

    /// <summary>
    /// The RPE an effort rating implies, for a set the user logged without one. A rating already says
    /// how the work felt, so asking for the same judgement twice on every row is a tax on logging —
    /// and a set with no RPE at all tells the analytics nothing. Null for the statuses that make no
    /// claim about effort, which is what clears a previously implied value back off the set.
    /// </summary>
    public static int? ImpliedRpe(string? status) => status switch
    {
        Easy => 5,
        Medium => 7,
        Hard => 9,
        _ => null
    };

    public static bool IsKnown(string? status) => status != null && All.Contains(status);

    /// <summary>
    /// The WDM-54 rule as something EF can translate, for queries rooted at
    /// <see cref="WorkoutExercise"/>. <see cref="WorkoutExercise.IsPerformed"/> is the same rule for
    /// collections already in memory; the two must not drift apart.
    /// </summary>
    public static readonly Expression<Func<WorkoutExercise, bool>> PerformedPredicate =
        workoutExercise => workoutExercise.Sets.Any()
            || workoutExercise.Status == Easy
            || workoutExercise.Status == Medium
            || workoutExercise.Status == Hard;
}
