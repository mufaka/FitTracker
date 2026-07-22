using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitTracker.Tests.Services;

/// <summary>
/// Phase 5: starting a workout from a plan, exercise status, and the derived statistics that must
/// stop treating a <see cref="WorkoutExercise"/> row as proof the exercise was trained.
/// </summary>
public class GuidedWorkoutTests
{
    [Fact]
    public async Task StartWorkoutFromPlanAsync_MaterializesPendingRowsInPlanOrderWithNoPrescriptionCopied()
    {
        // WDM-TEST-06.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day",
            (fixture.SquatId, 5, 5),
            (fixture.RunId, null, null),
            (fixture.PlankId, 3, null));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);

        Assert.NotNull(workout);
        Assert.Equal(planId, workout!.WorkoutPlanId);

        var materialized = workout.WorkoutExercises.OrderBy(we => we.Order).ToList();
        Assert.Equal(new[] { fixture.SquatId, fixture.RunId, fixture.PlankId }, materialized.Select(we => we.ExerciseId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, materialized.Select(we => we.Order).ToArray());
        Assert.All(materialized, we => Assert.Equal(WorkoutExerciseStatuses.Pending, we.Status));

        // Identity and order only — nothing that could later be mistaken for performed work.
        Assert.All(materialized, we => Assert.Empty(we.Sets));
        Assert.All(materialized, we => Assert.Null(we.Notes));
    }

    [Theory]
    [InlineData(false, true, false)]  // inactive
    [InlineData(true, false, false)]  // soft-deleted
    [InlineData(true, true, true)]    // owned by somebody else
    public async Task StartWorkoutFromPlanAsync_ReturnsNullForAPlanThatCannotGuide(bool isActive, bool notDeleted, bool otherUser)
    {
        // WDM-TEST-05.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var owner = otherUser ? fixture.Theirs : fixture.Mine;
        var planId = await CreatePlanAsync(context, owner, "Leg day", (fixture.SquatId, 5, 5));

        var plan = await context.WorkoutPlans.SingleAsync(p => p.Id == planId);
        plan.IsActive = isActive;
        plan.IsDeleted = !notDeleted;
        await context.SaveChangesAsync();

        var service = new WorkoutService(context);

        Assert.Null(await service.StartWorkoutFromPlanAsync(planId, fixture.Mine));

        // And no workout was created as a side effect of the refusal.
        Assert.False(await context.Workouts.AnyAsync(w => w.WorkoutPlanId == planId));
    }

    [Fact]
    public async Task EditingAPlanChangesItsGuidanceButNeverARecordedSet()
    {
        // WDM-TEST-07.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 5, 5));

        var workoutService = new WorkoutService(context);
        var planService = new WorkoutPlanService(context);

        var workout = await workoutService.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;
        Assert.True(await workoutService.LogSetAsync(workoutExerciseId, fixture.Mine, 100m, 5, null));

        var before = await context.Sets.AsNoTracking().SingleAsync(s => s.WorkoutExerciseId == workoutExerciseId);

        // Re-prescribe the plan entirely.
        var editor = await planService.GetPlanEditorAsync(planId, fixture.Mine);
        editor!.Exercises[0].TargetSets = 3;
        editor.Exercises[0].TargetReps = 12;
        Assert.NotNull(await planService.SavePlanAsync(fixture.Mine, editor));

        var after = await context.Sets.AsNoTracking().SingleAsync(s => s.WorkoutExerciseId == workoutExerciseId);
        Assert.Equal(before.Weight, after.Weight);
        Assert.Equal(before.Reps, after.Reps);

        // The guidance the workout now displays is the new prescription, read live.
        var guidance = await planService.GetPlanForGuidanceAsync(planId, fixture.Mine);
        Assert.Equal(3, guidance!.Exercises.Single().TargetSets);
        Assert.Equal(12, guidance.Exercises.Single().TargetReps);
    }

    [Fact]
    public async Task SetExerciseStatusAsync_RefusesSkippedOnceSetsExistAndLoggingASetUnskips()
    {
        // WDM-TEST-14.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 5, 5));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;

        // Skipping something untouched is fine.
        Assert.True(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, WorkoutExerciseStatuses.Skipped));
        Assert.Equal(WorkoutExerciseStatuses.Skipped, await StatusOfAsync(context, workoutExerciseId));

        // Recording work against it contradicts the mark, so the mark goes.
        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, 100m, 5, null));
        Assert.Equal(WorkoutExerciseStatuses.Pending, await StatusOfAsync(context, workoutExerciseId));

        // And it can no longer be skipped while that set stands.
        Assert.False(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, WorkoutExerciseStatuses.Skipped));
        Assert.Equal(WorkoutExerciseStatuses.Pending, await StatusOfAsync(context, workoutExerciseId));

        // An effort rating is still allowed.
        Assert.True(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, WorkoutExerciseStatuses.Hard));
        Assert.Equal(WorkoutExerciseStatuses.Hard, await StatusOfAsync(context, workoutExerciseId));
    }

    [Fact]
    public async Task SetExerciseStatusAsync_RejectsAnUnknownStatusAndAnotherUsersWorkout()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 5, 5));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;

        Assert.False(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, "Crushed"));
        Assert.False(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Theirs, WorkoutExerciseStatuses.Hard));
        Assert.Equal(WorkoutExerciseStatuses.Pending, await StatusOfAsync(context, workoutExerciseId));
    }

    [Fact]
    public async Task CompleteWorkoutAsync_NeedsOnePerformedExerciseNotMerelyOneRow()
    {
        // WDM-TEST-16. Eager materialization made the old "has any exercise" guard vacuous.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day",
            (fixture.SquatId, 5, 5),
            (fixture.RunId, null, null));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var rows = workout!.WorkoutExercises.OrderBy(we => we.Order).ToList();

        // Everything pending: nothing was done, so there is nothing to complete.
        var pendingResult = await service.CompleteWorkoutAsync(workout.Id, fixture.Mine, null);
        Assert.False(pendingResult.Succeeded);

        // Skipping them all is still nothing done.
        foreach (var row in rows)
            Assert.True(await service.SetExerciseStatusAsync(row.Id, fixture.Mine, WorkoutExerciseStatuses.Skipped));

        var skippedResult = await service.CompleteWorkoutAsync(workout.Id, fixture.Mine, null);
        Assert.False(skippedResult.Succeeded);

        // One rated exercise is enough, even with no sets recorded.
        Assert.True(await service.SetExerciseStatusAsync(rows[0].Id, fixture.Mine, WorkoutExerciseStatuses.Medium));
        Assert.True((await service.CompleteWorkoutAsync(workout.Id, fixture.Mine, null)).Succeeded);
    }

    [Fact]
    public async Task CompleteWorkoutAsync_AcceptsASingleRecordedSetAsProofOfWork()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 5, 5));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);

        Assert.True(await service.LogSetAsync(workout!.WorkoutExercises.Single().Id, fixture.Mine, 100m, 5, null));
        Assert.True((await service.CompleteWorkoutAsync(workout.Id, fixture.Mine, null)).Succeeded);
    }

    [Fact]
    public async Task SkippedAndUntouchedExercisesAreExcludedFromDerivedStatistics()
    {
        // WDM-TEST-15: muscle-group focus, recently-performed detection and last-performed lookups.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);

        // One completed workout: the squat was performed, the run was skipped, the plank untouched.
        var workout = new Workout
        {
            UserId = fixture.Mine,
            Date = DateTime.UtcNow.AddDays(-2),
            Duration = 40,
            IsCompleted = true,
            WorkoutExercises =
            {
                new WorkoutExercise
                {
                    ExerciseId = fixture.SquatId,
                    Order = 1,
                    Status = WorkoutExerciseStatuses.Pending,
                    Sets = { new Set { SetNumber = 1, Weight = 100m, Reps = 5 } }
                },
                new WorkoutExercise { ExerciseId = fixture.RunId, Order = 2, Status = WorkoutExerciseStatuses.Skipped },
                new WorkoutExercise { ExerciseId = fixture.PlankId, Order = 3, Status = WorkoutExerciseStatuses.Pending }
            }
        };
        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        // Last-performed, via the exercise library.
        var exerciseService = new ExerciseService(context);
        var squatHistory = await exerciseService.GetExerciseHistoryForUserAsync(fixture.SquatId, fixture.Mine);
        var runHistory = await exerciseService.GetExerciseHistoryForUserAsync(fixture.RunId, fixture.Mine);
        var plankHistory = await exerciseService.GetExerciseHistoryForUserAsync(fixture.PlankId, fixture.Mine);

        Assert.Equal(1, squatHistory.UsageCount);
        Assert.NotNull(squatHistory.LastPerformed);
        Assert.Equal(0, runHistory.UsageCount);
        Assert.Null(runHistory.LastPerformed);
        Assert.Equal(0, plankHistory.UsageCount);
        Assert.Null(plankHistory.LastPerformed);

        // The day's summary names only what was trained.
        var analytics = new AnalyticsService(context);
        var summary = await analytics.GetDailySummaryAsync(fixture.Mine, DateTime.UtcNow.AddDays(-2).Date);
        Assert.Contains("Back Squat", summary.ExercisesCompleted);
        Assert.DoesNotContain("Running", summary.ExercisesCompleted);
        Assert.DoesNotContain("Plank", summary.ExercisesCompleted);

        // Muscle-group focus: the skipped run must not make Legs look worked beyond the squat, and
        // the untouched plank must leave Core looking untrained.
        var suggestions = new WorkoutSuggestionService(context);
        var result = await suggestions.GetSuggestionsAsync(fixture.Mine, 28);
        Assert.Contains("Core", result.FocusMuscleGroups);
    }

    [Theory]
    [InlineData(WorkoutExerciseStatuses.Easy, 5)]
    [InlineData(WorkoutExerciseStatuses.Medium, 7)]
    [InlineData(WorkoutExerciseStatuses.Hard, 9)]
    public async Task RatingAnExerciseSuppliesTheRpeForSetsLoggedWithoutOne(string effort, int expected)
    {
        // WDM-58. The rating already says how the work felt; asking again per row is a tax on logging.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 2, 5));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;

        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, 100m, 5, null));
        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, 100m, 5, rpe: 8));

        Assert.True(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, effort));

        var sets = await SetsOfAsync(context, workoutExerciseId);
        Assert.Equal(expected, sets[0].RPE);
        Assert.True(sets[0].IsRpeDerived);

        // What the user typed is theirs and stands.
        Assert.Equal(8, sets[1].RPE);
        Assert.False(sets[1].IsRpeDerived);
    }

    [Fact]
    public async Task ChangingOrClearingTheRatingMovesOnlyTheRpeItSupplied()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 2, 5));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;

        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, 100m, 5, null));
        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, 100m, 5, rpe: 8));

        Assert.True(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, WorkoutExerciseStatuses.Easy));
        Assert.Equal(5, (await SetsOfAsync(context, workoutExerciseId))[0].RPE);

        // Re-rating carries the derived value with it.
        Assert.True(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, WorkoutExerciseStatuses.Hard));
        var reRated = await SetsOfAsync(context, workoutExerciseId);
        Assert.Equal(9, reRated[0].RPE);
        Assert.Equal(8, reRated[1].RPE);

        // Clearing the answer really does undo it, rather than leaving a number nobody chose.
        Assert.True(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, WorkoutExerciseStatuses.Pending));
        var cleared = await SetsOfAsync(context, workoutExerciseId);
        Assert.Null(cleared[0].RPE);
        Assert.False(cleared[0].IsRpeDerived);
        Assert.Equal(8, cleared[1].RPE);
    }

    [Fact]
    public async Task LogSetAsync_TakesTheRpeOfARatingAlreadyGiven()
    {
        // The full list lets an exercise be rated before anything is logged against it.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 2, 5));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;

        Assert.True(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, WorkoutExerciseStatuses.Medium));
        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, 100m, 5, null));
        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, 100m, 5, rpe: 4));

        var sets = await SetsOfAsync(context, workoutExerciseId);
        Assert.Equal(7, sets[0].RPE);
        Assert.True(sets[0].IsRpeDerived);
        Assert.Equal(4, sets[1].RPE);
        Assert.False(sets[1].IsRpeDerived);
    }

    [Fact]
    public async Task SkippingLeavesNoRpeBehind()
    {
        // Skipped implies no effort, so it supplies nothing — and there are no sets to supply it to.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 2, 5));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;

        Assert.True(await service.SetExerciseStatusAsync(workoutExerciseId, fixture.Mine, WorkoutExerciseStatuses.Skipped));
        Assert.Null(WorkoutExerciseStatuses.ImpliedRpe(WorkoutExerciseStatuses.Skipped));
        Assert.Empty(await SetsOfAsync(context, workoutExerciseId));
    }

    private static async Task<List<Set>> SetsOfAsync(ApplicationDbContext context, int workoutExerciseId) =>
        await context.Sets.AsNoTracking()
            .Where(s => s.WorkoutExerciseId == workoutExerciseId)
            .OrderBy(s => s.SetNumber)
            .ToListAsync();

    [Fact]
    public async Task LogSetAsync_NumbersANewSetPastTheHighestRatherThanPastTheCount()
    {
        // Sets are not renumbered when one is removed, so counting them would reissue a number that
        // is still in use — two sets called 3, and a logger that cannot tell them apart.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Leg day", (fixture.SquatId, 3, 5));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;

        foreach (var weight in new[] { 100m, 105m, 110m })
            Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, weight, 5, null));

        var first = await context.Sets.AsNoTracking().SingleAsync(s => s.WorkoutExerciseId == workoutExerciseId && s.SetNumber == 1);
        Assert.True(await service.RemoveSetAsync(first.Id, fixture.Mine));

        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, 115m, 5, null));

        var numbers = await context.Sets.AsNoTracking()
            .Where(s => s.WorkoutExerciseId == workoutExerciseId)
            .Select(s => s.SetNumber)
            .OrderBy(n => n)
            .ToListAsync();

        Assert.Equal(new[] { 2, 3, 4 }, numbers);
    }

    [Fact]
    public async Task LogSetAsync_StoresDurationVerbatimAndDistanceCanonically()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var planId = await CreatePlanAsync(context, fixture.Mine, "Easy run", (fixture.RunId, null, null));

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutFromPlanAsync(planId, fixture.Mine);
        var workoutExerciseId = workout!.WorkoutExercises.Single().Id;

        // An lbs user enters miles.
        Assert.True(await service.LogSetAsync(workoutExerciseId, fixture.Mine, null, null, null, durationSeconds: 1800, distance: 3.11m));

        var set = await context.Sets.AsNoTracking().SingleAsync(s => s.WorkoutExerciseId == workoutExerciseId);
        Assert.Equal(1800, set.Duration);
        Assert.Equal(5.0051m, set.Distance);
        Assert.Null(set.Weight);
        Assert.Null(set.Reps);

        // And it reads back as the number that was typed.
        Assert.Equal(3.11m, UnitConverter.ToDisplayDistance(set.Distance, "lbs"));
    }

    private static async Task<string> StatusOfAsync(ApplicationDbContext context, int workoutExerciseId) =>
        await context.WorkoutExercises.AsNoTracking().Where(we => we.Id == workoutExerciseId).Select(we => we.Status).SingleAsync();

    private static async Task<int> CreatePlanAsync(
        ApplicationDbContext context,
        string userId,
        string name,
        params (int ExerciseId, int? Sets, int? Reps)[] exercises)
    {
        var plan = new WorkoutPlan
        {
            UserId = userId,
            Name = name,
            Exercises = exercises
                .Select((e, index) => new WorkoutPlanExercise
                {
                    ExerciseId = e.ExerciseId,
                    Order = index + 1,
                    TargetSets = e.Sets,
                    TargetReps = e.Reps
                })
                .ToList()
        };

        context.WorkoutPlans.Add(plan);
        await context.SaveChangesAsync();
        return plan.Id;
    }

    private static async Task<Fixture> SeedAsync(ApplicationDbContext context)
    {
        var mine = CreateUser("user-guided-mine");
        var theirs = CreateUser("user-guided-theirs");
        var squat = new Exercise { Name = "Back Squat", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Legs" };
        var run = new Exercise { Name = "Running", Category = "Cardio", Equipment = "None", MuscleGroups = "Legs" };
        var plank = new Exercise { Name = "Plank", Category = "Core", Equipment = "Bodyweight", MuscleGroups = "Core" };

        context.Users.AddRange(mine, theirs);
        context.Exercises.AddRange(squat, run, plank);
        await context.SaveChangesAsync();

        return new Fixture(mine.Id, theirs.Id, squat.Id, run.Id, plank.Id);
    }

    private sealed record Fixture(string Mine, string Theirs, int SquatId, int RunId, int PlankId);

    private static ApplicationUser CreateUser(string id) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}
