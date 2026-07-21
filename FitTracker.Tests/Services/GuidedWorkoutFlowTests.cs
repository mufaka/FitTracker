using FitTracker.Models;
using FitTracker.Services;
using Xunit;

namespace FitTracker.Tests.Services;

/// <summary>
/// The order the guided flow walks a workout in. Pure and database-free, like the helper itself.
/// </summary>
public class GuidedWorkoutFlowTests
{
    [Fact]
    public void NextUnaddressed_StartsAtTheFirstOpenExerciseWhenNothingHasBeenAskedFor()
    {
        var ordered = new[]
        {
            Performed(1),
            Skipped(2),
            Pending(3),
            Pending(4)
        };

        Assert.Equal(3, GuidedWorkoutFlow.NextUnaddressed(ordered)!.Id);
    }

    [Fact]
    public void NextUnaddressed_WrapsPastTheEndSoAnExercisePassedOverEarlierComesBackRound()
    {
        // The user jumped ahead to the last exercise and answered for it; exercise 1 is still open,
        // and the flow must return to it rather than declaring the workout finished.
        var ordered = new[]
        {
            Pending(1),
            Performed(2),
            Performed(3)
        };

        Assert.Equal(1, GuidedWorkoutFlow.NextUnaddressed(ordered, afterId: 3)!.Id);
    }

    [Fact]
    public void NextUnaddressed_OffersTheExerciseJustLeftOnlyAfterEverythingElse()
    {
        // Clearing a status leaves the current exercise open. Moving on should mean moving on, so it
        // is offered again last — and only because nothing else is waiting.
        var ordered = new[]
        {
            Performed(1),
            Pending(2),
            Pending(3)
        };

        Assert.Equal(3, GuidedWorkoutFlow.NextUnaddressed(ordered, afterId: 2)!.Id);

        var onlyOneLeft = new[] { Performed(1), Pending(2), Performed(3) };
        Assert.Equal(2, GuidedWorkoutFlow.NextUnaddressed(onlyOneLeft, afterId: 2)!.Id);
    }

    [Fact]
    public void NextUnaddressed_ReturnsNullOnceEveryExerciseHasBeenAnsweredFor()
    {
        var ordered = new[] { Performed(1), Skipped(2), Rated(3) };

        Assert.Null(GuidedWorkoutFlow.NextUnaddressed(ordered));
        Assert.Null(GuidedWorkoutFlow.NextUnaddressed(ordered, afterId: 2));
        Assert.Null(GuidedWorkoutFlow.NextUnaddressed(Array.Empty<WorkoutExercise>()));
    }

    [Fact]
    public void NextUnaddressed_TreatsAnIdThatIsNoLongerInTheWorkoutAsNoStartingPoint()
    {
        var ordered = new[] { Pending(1), Pending(2) };

        Assert.Equal(1, GuidedWorkoutFlow.NextUnaddressed(ordered, afterId: 99)!.Id);
    }

    [Fact]
    public void Resolve_ShowsTheExerciseAskedForEvenWhenItIsAlreadyDone()
    {
        // WDM-UI-08: the flow proposes an order, it does not impose one. Stepping back to something
        // already logged has to show that exercise, not bounce to the next open one.
        var ordered = new[] { Performed(1), Pending(2) };

        Assert.Equal(1, GuidedWorkoutFlow.Resolve(ordered, requestedId: 1)!.Id);
    }

    [Fact]
    public void Resolve_FallsBackToTheNextOpenExerciseWhenTheRequestIsStaleOrAbsent()
    {
        var ordered = new[] { Performed(1), Pending(2) };

        Assert.Equal(2, GuidedWorkoutFlow.Resolve(ordered, requestedId: null)!.Id);
        Assert.Equal(2, GuidedWorkoutFlow.Resolve(ordered, requestedId: 99)!.Id);
        Assert.Null(GuidedWorkoutFlow.Resolve(new[] { Performed(1) }, requestedId: null));
    }

    [Fact]
    public void AddressedCount_CountsRecordedSetsAndEffortRatingsAndSkipsButNotBareRows()
    {
        // The same WDM-54 rule the statistics use: a materialized row proves nothing on its own.
        var ordered = new[] { Performed(1), Rated(2), Skipped(3), Pending(4) };

        Assert.Equal(3, GuidedWorkoutFlow.AddressedCount(ordered));
        Assert.False(GuidedWorkoutFlow.IsAddressed(ordered[3]));
    }

    private static WorkoutExercise Pending(int id) => new()
    {
        Id = id,
        Order = id,
        Status = WorkoutExerciseStatuses.Pending
    };

    private static WorkoutExercise Skipped(int id) => new()
    {
        Id = id,
        Order = id,
        Status = WorkoutExerciseStatuses.Skipped
    };

    private static WorkoutExercise Rated(int id) => new()
    {
        Id = id,
        Order = id,
        Status = WorkoutExerciseStatuses.Medium
    };

    private static WorkoutExercise Performed(int id) => new()
    {
        Id = id,
        Order = id,
        Status = WorkoutExerciseStatuses.Pending,
        Sets = { new Set { SetNumber = 1, Weight = 60m, Reps = 5 } }
    };
}
