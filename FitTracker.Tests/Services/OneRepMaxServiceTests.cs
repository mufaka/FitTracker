using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Xunit;

namespace FitTracker.Tests.Services;

public class OneRepMaxServiceTests
{
    private static readonly DateTime Today = new(2026, 7, 21);

    [Fact]
    public async Task GetLeaderboardAsync_RanksTrackedExercisesByBestEstimate()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var squat = CreateExercise("Barbell Back Squat", tracksOneRepMax: true);
        var curl = CreateExercise("Barbell Curl", tracksOneRepMax: true);

        context.Users.Add(user);
        context.Exercises.AddRange(squat, curl);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, squat, Today.AddDays(-10), (225m, 5)),
            CreateWorkout(user.Id, curl, Today.AddDays(-9), (65m, 8)));
        await context.SaveChangesAsync();

        var leaderboard = await new OneRepMaxService(context).GetLeaderboardAsync(user.Id, Today);

        Assert.Equal(2, leaderboard.Entries.Count);
        Assert.Equal("Barbell Back Squat", leaderboard.Entries[0].ExerciseName);
        Assert.Equal(1, leaderboard.Entries[0].Rank);
        Assert.Equal(259.97m, leaderboard.Entries[0].BestOneRepMax);
        Assert.Equal(225m, leaderboard.Entries[0].BestWeight);
        Assert.Equal(5, leaderboard.Entries[0].BestReps);
        Assert.Equal("Barbell Curl", leaderboard.Entries[1].ExerciseName);
        Assert.Equal(2, leaderboard.Entries[1].Rank);
        Assert.Equal(259.97m, leaderboard.HeaviestOneRepMax);
    }

    [Fact]
    public async Task GetLeaderboardAsync_IgnoresExercisesThatDoNotTrackOneRepMax()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var bench = CreateExercise("Barbell Bench Press", tracksOneRepMax: true);

        // A weighted plank is still a plank: a number in the weight column does not make
        // a one-rep max meaningful, which is exactly what the flag is for.
        var plank = CreateExercise("Plank", tracksOneRepMax: false);

        context.Users.Add(user);
        context.Exercises.AddRange(bench, plank);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, bench, Today.AddDays(-5), (185m, 5)),
            CreateWorkout(user.Id, plank, Today.AddDays(-4), (405m, 5)));
        await context.SaveChangesAsync();

        var leaderboard = await new OneRepMaxService(context).GetLeaderboardAsync(user.Id, Today);

        var entry = Assert.Single(leaderboard.Entries);
        Assert.Equal("Barbell Bench Press", entry.ExerciseName);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ExcludesOtherUsersAndIncompleteWorkouts()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var otherUser = CreateUser("user-2");
        var bench = CreateExercise("Barbell Bench Press", tracksOneRepMax: true);

        var abandoned = CreateWorkout(user.Id, bench, Today.AddDays(-1), (315m, 5));
        abandoned.IsCompleted = false;

        context.Users.AddRange(user, otherUser);
        context.Exercises.Add(bench);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, bench, Today.AddDays(-5), (185m, 5)),
            CreateWorkout(otherUser.Id, bench, Today.AddDays(-3), (405m, 5)),
            abandoned);
        await context.SaveChangesAsync();

        var leaderboard = await new OneRepMaxService(context).GetLeaderboardAsync(user.Id, Today);

        var entry = Assert.Single(leaderboard.Entries);
        Assert.Equal(1, entry.SessionCount);
        Assert.Equal(213.75m, entry.BestOneRepMax);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReportsChangeAcrossTheTrendWindow()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var bench = CreateExercise("Barbell Bench Press", tracksOneRepMax: true);

        context.Users.Add(user);
        context.Exercises.Add(bench);
        context.Workouts.AddRange(
            // Outside the window, so it must not become the baseline.
            CreateWorkout(user.Id, bench, Today.AddDays(-OneRepMaxService.TrendWindowDays - 30), (95m, 5)),
            CreateWorkout(user.Id, bench, Today.AddDays(-40), (185m, 5)),
            CreateWorkout(user.Id, bench, Today.AddDays(-5), (205m, 5)));
        await context.SaveChangesAsync();

        var leaderboard = await new OneRepMaxService(context).GetLeaderboardAsync(user.Id, Today);

        var entry = Assert.Single(leaderboard.Entries);
        Assert.Equal(3, entry.SessionCount);
        Assert.Equal(236.86m, entry.CurrentOneRepMax);
        Assert.Equal(236.86m - 213.75m, entry.RecentChange);
    }

    [Fact]
    public async Task GetExerciseTrendAsync_UsesTheBestUsableSetPerSessionAndMarksNewBests()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var bench = CreateExercise("Barbell Bench Press", tracksOneRepMax: true);

        context.Users.Add(user);
        context.Exercises.Add(bench);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, bench, Today.AddDays(-20), (185m, 5)),
            // The heaviest set here is a 12-rep grinder that cannot be estimated from; the
            // session's 1RM has to come from the lighter set that can.
            CreateWorkout(user.Id, bench, Today.AddDays(-10), (225m, 12), (175m, 5)),
            CreateWorkout(user.Id, bench, Today.AddDays(-2), (205m, 5)));
        await context.SaveChangesAsync();

        var trend = await new OneRepMaxService(context).GetExerciseTrendAsync(user.Id, bench.Id, Today);

        Assert.NotNull(trend);
        Assert.Equal(3, trend!.Points.Count);
        Assert.Equal(175m, trend.Points[1].Weight);
        Assert.Equal(5, trend.Points[1].Reps);
        Assert.Equal(236.86m, trend.CurrentOneRepMax);
        Assert.Equal(236.86m, trend.BestOneRepMax);
        Assert.Equal(Today.AddDays(-2), trend.BestAchievedOn);

        // First session is a best by default; the dip in the middle is not; the last one is.
        Assert.Equal(new[] { true, false, true }, trend.Points.Select(point => point.IsPersonalBest));
    }

    [Fact]
    public async Task GetExerciseTrendAsync_ProjectsForwardOnAnEstablishedTrend()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var bench = CreateExercise("Barbell Bench Press", tracksOneRepMax: true);

        context.Users.Add(user);
        context.Exercises.Add(bench);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, bench, Today.AddDays(-42), (100m, 5)),
            CreateWorkout(user.Id, bench, Today.AddDays(-28), (105m, 5)),
            CreateWorkout(user.Id, bench, Today.AddDays(-14), (110m, 5)),
            CreateWorkout(user.Id, bench, Today, (115m, 5)));
        await context.SaveChangesAsync();

        var trend = await new OneRepMaxService(context).GetExerciseTrendAsync(user.Id, bench.Id, Today);

        var projection = trend!.Projection;
        Assert.NotNull(projection);
        Assert.Equal(4, projection!.SampleCount);
        Assert.True(projection.IsImproving);
        Assert.True(projection.IsReliable);
        Assert.InRange(projection.ChangePerWeek, 2.8m, 3.0m);
        Assert.Equal(new[] { 30, 60, 90 }, projection.Horizons.Select(horizon => horizon.Days));
        Assert.Equal(Today.AddDays(30), projection.Horizons[0].Date);
        Assert.InRange(projection.Horizons[0].OneRepMax, 144m, 147m);
        Assert.True(projection.Horizons[2].OneRepMax > projection.Horizons[0].OneRepMax);
    }

    [Fact]
    public async Task GetExerciseTrendAsync_WithholdsProjectionFromASingleWeekOfWork()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var bench = CreateExercise("Barbell Bench Press", tracksOneRepMax: true);

        context.Users.Add(user);
        context.Exercises.Add(bench);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, bench, Today.AddDays(-4), (100m, 5)),
            CreateWorkout(user.Id, bench, Today.AddDays(-2), (110m, 5)),
            CreateWorkout(user.Id, bench, Today, (120m, 5)));
        await context.SaveChangesAsync();

        var trend = await new OneRepMaxService(context).GetExerciseTrendAsync(user.Id, bench.Id, Today);

        Assert.NotNull(trend);
        Assert.Equal(3, trend!.Points.Count);
        Assert.Null(trend.Projection);
    }

    [Fact]
    public async Task GetExerciseTrendAsync_ReturnsNullWhenTheExerciseHasNoOneRepMax()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var running = CreateExercise("Running", tracksOneRepMax: false);

        context.Users.Add(user);
        context.Exercises.Add(running);
        await context.SaveChangesAsync();

        var service = new OneRepMaxService(context);

        Assert.Null(await service.GetExerciseTrendAsync(user.Id, running.Id, Today));
        Assert.Null(await service.GetExerciseTrendAsync(user.Id, exerciseId: 9999, Today));
    }

    [Fact]
    public async Task GetExerciseTrendAsync_ReturnsAnEmptyHistoryForATrackedExerciseNeverLogged()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var bench = CreateExercise("Barbell Bench Press", tracksOneRepMax: true);

        context.Users.Add(user);
        context.Exercises.Add(bench);
        await context.SaveChangesAsync();

        var trend = await new OneRepMaxService(context).GetExerciseTrendAsync(user.Id, bench.Id, Today);

        Assert.NotNull(trend);
        Assert.False(trend!.HasHistory);
        Assert.Equal(0m, trend.BestOneRepMax);
        Assert.Null(trend.Projection);
    }

    private static Workout CreateWorkout(string userId, Exercise exercise, DateTime date, params (decimal Weight, int Reps)[] sets)
    {
        var workoutExercise = new WorkoutExercise { Exercise = exercise, Order = 1 };

        for (var index = 0; index < sets.Length; index++)
        {
            workoutExercise.Sets.Add(new Set
            {
                SetNumber = index + 1,
                Weight = sets[index].Weight,
                Reps = sets[index].Reps
            });
        }

        return new Workout
        {
            UserId = userId,
            Date = date,
            Duration = 45,
            IsCompleted = true,
            WorkoutExercises = { workoutExercise }
        };
    }

    private static Exercise CreateExercise(string name, bool tracksOneRepMax) => new()
    {
        Name = name,
        Category = "Strength",
        Equipment = "Barbell",
        MuscleGroups = "Chest",
        TracksOneRepMax = tracksOneRepMax
    };

    private static ApplicationUser CreateUser(string id = "user-1") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}
