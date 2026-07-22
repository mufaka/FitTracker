using FitTracker.Models;

namespace FitTracker.Services;

/// <summary>
/// Which exercise the guided flow puts in front of the user next, and how much of the workout is
/// still waiting on an answer. Database-free like <see cref="UnitConverter"/> — it reasons over
/// exercises the page has already loaded — so the rules can be pinned by tests without a context.
/// </summary>
public static class GuidedWorkoutFlow
{
    /// <summary>
    /// Whether the user has answered for this exercise: performed it (WDM-54) or deliberately passed
    /// on it. Pending with nothing logged is the only open state, and working through those is the
    /// whole of what the flow does.
    /// </summary>
    public static bool IsAddressed(WorkoutExercise workoutExercise) =>
        workoutExercise.IsPerformed || workoutExercise.Status == WorkoutExerciseStatuses.Skipped;

    /// <summary>How many of <paramref name="ordered"/> have been answered for.</summary>
    public static int AddressedCount(IReadOnlyList<WorkoutExercise> ordered) =>
        ordered.Count(IsAddressed);

    /// <summary>
    /// The next exercise still waiting on an answer, starting just past <paramref name="afterId"/> and
    /// wrapping to the front — so one passed over earlier comes back round before the workout is
    /// declared finished, and the exercise just answered for is the last thing offered again rather
    /// than the first. Null once every exercise has been answered for.
    /// </summary>
    public static WorkoutExercise? NextUnaddressed(IReadOnlyList<WorkoutExercise> ordered, int? afterId = null)
    {
        // -1 for "nowhere in particular", which starts the sweep at the first exercise. An id that is
        // no longer in the workout — removed, or another workout's — lands here too.
        var position = -1;
        for (var i = 0; afterId.HasValue && i < ordered.Count; i++)
        {
            if (ordered[i].Id == afterId.Value)
            {
                position = i;
                break;
            }
        }

        for (var offset = 1; offset <= ordered.Count; offset++)
        {
            var candidate = ordered[(position + offset) % ordered.Count];
            if (!IsAddressed(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// The exercise to show: the one asked for while it is still part of the workout — the user may
    /// step back to anything, in any order (WDM-UI-08) — otherwise whatever is next unanswered. Null
    /// means there is nothing left to put in front of the user, which is what ends the flow.
    /// </summary>
    public static WorkoutExercise? Resolve(IReadOnlyList<WorkoutExercise> ordered, int? requestedId)
    {
        var requested = requestedId.HasValue
            ? ordered.FirstOrDefault(we => we.Id == requestedId.Value)
            : null;

        return requested ?? NextUnaddressed(ordered);
    }
}
