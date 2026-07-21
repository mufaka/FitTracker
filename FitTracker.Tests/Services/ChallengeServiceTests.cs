using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Xunit;

namespace FitTracker.Tests.Services;

public class ChallengeServiceTests
{
    [Fact]
    public async Task GetChallengeOverviewAsync_CountsOnlyWorkoutsInsideTheWindow()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise();
        var challenge = CreateChallenge("Workout Sprint", ChallengeGoalTypes.CompletedWorkouts, goal: 5m, durationDays: 7);

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var today = new DateTime(2026, 5, 20);
        var startedDate = today.AddDays(-3);

        context.UserChallenges.Add(new UserChallenge
        {
            UserId = user.Id,
            ChallengeId = challenge.Id,
            StartedDate = startedDate
        });

        // Two inside the window, one the day before it opened, one after it closes.
        context.Workouts.AddRange(
            CreateWorkout(user.Id, exercise, startedDate, sets: 2),
            CreateWorkout(user.Id, exercise, startedDate.AddDays(2), sets: 2),
            CreateWorkout(user.Id, exercise, startedDate.AddDays(-1), sets: 2),
            CreateWorkout(user.Id, exercise, startedDate.AddDays(7), sets: 2));
        await context.SaveChangesAsync();

        var service = new ChallengeService(context);
        var summary = await service.GetChallengeOverviewAsync(user.Id, today);

        var item = Assert.Single(summary.Challenges);
        Assert.True(item.HasJoined);
        Assert.Equal(2m, item.CurrentValue);
        Assert.Equal(5m, item.TargetValue);
        Assert.Equal(40, item.ProgressPercentage);
        Assert.Equal(4, item.DaysRemaining);
        Assert.True(item.IsActive);
    }

    [Fact]
    public async Task GetChallengeOverviewAsync_SumsVolumeInsideTheWindow()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser("user-challenge-volume");
        var exercise = CreateExercise();
        var challenge = CreateChallenge("Volume Push", ChallengeGoalTypes.TotalVolume, goal: 2000m, durationDays: 30);

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var today = new DateTime(2026, 5, 20);
        var startedDate = today.AddDays(-5);

        context.UserChallenges.Add(new UserChallenge
        {
            UserId = user.Id,
            ChallengeId = challenge.Id,
            StartedDate = startedDate
        });

        // 2 sets x 100kg x 5 reps = 1000 inside; the older workout must not count.
        context.Workouts.AddRange(
            CreateWorkout(user.Id, exercise, startedDate.AddDays(1), sets: 2),
            CreateWorkout(user.Id, exercise, startedDate.AddDays(-2), sets: 2));
        await context.SaveChangesAsync();

        var service = new ChallengeService(context);
        var summary = await service.GetChallengeOverviewAsync(user.Id, today);

        var item = Assert.Single(summary.Challenges);
        Assert.Equal(1000m, item.CurrentValue);
        Assert.Equal(50, item.ProgressPercentage);
    }

    [Fact]
    public async Task EvaluateChallengesAsync_CompletesWhenGoalReachedInsideWindow()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser("user-challenge-complete");
        var exercise = CreateExercise();
        var challenge = CreateChallenge("Two Workouts", ChallengeGoalTypes.CompletedWorkouts, goal: 2m, durationDays: 7);

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var today = new DateTime(2026, 5, 20);
        var startedDate = today.AddDays(-2);

        context.UserChallenges.Add(new UserChallenge
        {
            UserId = user.Id,
            ChallengeId = challenge.Id,
            StartedDate = startedDate
        });
        context.Workouts.AddRange(
            CreateWorkout(user.Id, exercise, startedDate, sets: 1),
            CreateWorkout(user.Id, exercise, startedDate.AddDays(1), sets: 1));
        await context.SaveChangesAsync();

        var service = new ChallengeService(context);
        var completed = await service.EvaluateChallengesAsync(user.Id, today);

        Assert.Single(completed);
        Assert.Equal(today, Assert.Single(completed).CompletedDate);

        var summary = await service.GetChallengeOverviewAsync(user.Id, today);
        var item = Assert.Single(summary.Challenges);
        Assert.True(item.IsCompleted);
        Assert.False(item.IsActive);

        // Evaluating again must not complete it a second time.
        Assert.Empty(await service.EvaluateChallengesAsync(user.Id, today));
    }

    [Fact]
    public async Task EvaluateChallengesAsync_DoesNotCompleteAfterTheWindowClosed()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser("user-challenge-expired");
        var exercise = CreateExercise();
        var challenge = CreateChallenge("Lapsed", ChallengeGoalTypes.CompletedWorkouts, goal: 1m, durationDays: 7);

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var today = new DateTime(2026, 5, 20);
        var startedDate = today.AddDays(-30);

        context.UserChallenges.Add(new UserChallenge
        {
            UserId = user.Id,
            ChallengeId = challenge.Id,
            StartedDate = startedDate
        });
        context.Workouts.Add(CreateWorkout(user.Id, exercise, startedDate.AddDays(1), sets: 1));
        await context.SaveChangesAsync();

        var service = new ChallengeService(context);

        Assert.Empty(await service.EvaluateChallengesAsync(user.Id, today));

        var summary = await service.GetChallengeOverviewAsync(user.Id, today);
        var item = Assert.Single(summary.Challenges);
        Assert.True(item.HasExpired);
        Assert.False(item.IsActive);
        Assert.False(item.IsCompleted);
        Assert.Equal(0, item.DaysRemaining);
    }

    [Fact]
    public async Task JoinChallengeAsync_StartsWindowTodayAndRejoiningRestartsIt()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser("user-challenge-join");
        var challenge = CreateChallenge("Restartable", ChallengeGoalTypes.CompletedWorkouts, goal: 3m, durationDays: 10);

        context.Users.Add(user);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var service = new ChallengeService(context);

        var firstStart = new DateTime(2026, 5, 1);
        Assert.True(await service.JoinChallengeAsync(user.Id, challenge.Id, firstStart));

        var afterJoin = await service.GetChallengeOverviewAsync(user.Id, firstStart);
        var joined = Assert.Single(afterJoin.Challenges);
        Assert.True(joined.IsActive);
        Assert.Equal(firstStart, joined.StartedDate);
        Assert.Equal(firstStart.AddDays(9), joined.EndDate);

        // Re-joining reuses the single row rather than violating the unique index.
        var secondStart = new DateTime(2026, 6, 1);
        Assert.True(await service.JoinChallengeAsync(user.Id, challenge.Id, secondStart));

        Assert.Single(context.UserChallenges.Where(userChallenge => userChallenge.UserId == user.Id));

        var afterRejoin = await service.GetChallengeOverviewAsync(user.Id, secondStart);
        var restarted = Assert.Single(afterRejoin.Challenges);
        Assert.Equal(secondStart, restarted.StartedDate);
        Assert.Equal(10, restarted.DaysRemaining);
    }

    [Fact]
    public async Task LeaveChallengeAsync_RemovesParticipationAndReturnsItToAvailable()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser("user-challenge-leave");
        var challenge = CreateChallenge("Abandonable", ChallengeGoalTypes.TotalSets, goal: 50m, durationDays: 14);

        context.Users.Add(user);
        context.Challenges.Add(challenge);
        await context.SaveChangesAsync();

        var service = new ChallengeService(context);
        var today = new DateTime(2026, 5, 20);

        await service.JoinChallengeAsync(user.Id, challenge.Id, today);
        Assert.True(await service.LeaveChallengeAsync(user.Id, challenge.Id));

        var summary = await service.GetChallengeOverviewAsync(user.Id, today);
        var item = Assert.Single(summary.Challenges);
        Assert.False(item.HasJoined);
        Assert.Single(summary.AvailableChallenges);

        // Leaving something never joined is a no-op rather than an error.
        Assert.False(await service.LeaveChallengeAsync(user.Id, challenge.Id));
    }

    private static Challenge CreateChallenge(string name, string goalType, decimal goal, int durationDays) => new()
    {
        Name = name,
        Description = $"{name} description",
        Icon = "🎯",
        GoalType = goalType,
        Goal = goal,
        DurationDays = durationDays
    };

    private static Workout CreateWorkout(string userId, Exercise exercise, DateTime date, int sets) => new()
    {
        UserId = userId,
        Date = date,
        Duration = 45,
        IsCompleted = true,
        WorkoutExercises =
        {
            new WorkoutExercise
            {
                Exercise = exercise,
                Order = 1,
                Sets = Enumerable.Range(1, sets)
                    .Select(setNumber => new Set { SetNumber = setNumber, Weight = 100m, Reps = 5 })
                    .ToList()
            }
        }
    };

    private static ApplicationUser CreateUser(string id = "user-challenge-1") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };

    private static Exercise CreateExercise() => new()
    {
        Name = "Back Squat",
        Category = "Strength",
        Equipment = "Barbell",
        MuscleGroups = "Legs"
    };
}
