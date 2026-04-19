using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Xunit;

namespace FitTracker.Tests.Services;

public class AchievementServiceTests
{
    [Fact]
    public async Task EvaluateAndUnlockAchievementsAsync_UnlocksWorkoutAndPrMilestones()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise("Bench Press", "Strength", "Barbell", "Chest");

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Achievements.AddRange(
            new Achievement { Name = "First Workout", Description = "", Icon = "🏁", Criteria = $"{AchievementCriteria.CompletedWorkouts}:1" },
            new Achievement { Name = "First PR", Description = "", Icon = "🏆", Criteria = $"{AchievementCriteria.PersonalRecords}:1" },
            new Achievement { Name = "100 Total Sets", Description = "", Icon = "💯", Criteria = $"{AchievementCriteria.TotalSets}:100" });

        var workout = new Workout
        {
            UserId = user.Id,
            Date = DateTime.UtcNow,
            Duration = 45,
            IsCompleted = true,
            WorkoutExercises =
            {
                new WorkoutExercise
                {
                    Exercise = exercise,
                    Order = 1,
                    Sets =
                    {
                        new Set { SetNumber = 1, Weight = 135m, Reps = 8 }
                    }
                }
            }
        };

        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        context.PersonalRecords.Add(new PersonalRecord
        {
            UserId = user.Id,
            ExerciseId = exercise.Id,
            WorkoutId = workout.Id,
            Date = workout.Date,
            Weight = 135m,
            Reps = 8,
            OneRepMax = 171m
        });
        await context.SaveChangesAsync();

        var service = new AchievementService(context);
        var unlocked = await service.EvaluateAndUnlockAchievementsAsync(user.Id, workout.Date);

        Assert.Equal(2, unlocked.Count);
        Assert.Contains(unlocked, item => item.Achievement.Name == "First Workout");
        Assert.Contains(unlocked, item => item.Achievement.Name == "First PR");
    }

    [Fact]
    public async Task GetAchievementOverviewAsync_ReturnsProgressForLockedAchievements()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser("user-achievements-2");
        var exercise = CreateExercise("Squat", "Strength", "Barbell", "Legs");

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Achievements.AddRange(
            new Achievement { Name = "10 Workouts", Description = "", Icon = "🔥", Criteria = $"{AchievementCriteria.CompletedWorkouts}:10" },
            new Achievement { Name = "100 Total Sets", Description = "", Icon = "💯", Criteria = $"{AchievementCriteria.TotalSets}:100" });

        context.Workouts.AddRange(
            CreateWorkout(user.Id, exercise, DateTime.UtcNow.AddDays(-2), 3),
            CreateWorkout(user.Id, exercise, DateTime.UtcNow.AddDays(-1), 2));

        await context.SaveChangesAsync();

        var service = new AchievementService(context);
        var summary = await service.GetAchievementOverviewAsync(user.Id);

        Assert.Equal(2, summary.LockedCount);
        Assert.Contains(summary.LockedAchievements, item => item.Name == "10 Workouts" && item.ProgressLabel.Contains("5 / 10"));
        Assert.Contains(summary.LockedAchievements, item => item.Name == "100 Total Sets" && item.ProgressLabel.Contains("5 / 100"));
    }

    private static Workout CreateWorkout(string userId, Exercise exercise, DateTime date, int setCount) => new()
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
                Sets = Enumerable.Range(1, setCount)
                    .Select(setNumber => new Set { SetNumber = setNumber, Weight = 100m, Reps = 5 })
                    .ToList()
            }
        }
    };

    private static ApplicationUser CreateUser(string id = "user-achievements-1") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };

    private static Exercise CreateExercise(string name, string category, string equipment, string muscleGroups) => new()
    {
        Name = name,
        Category = category,
        Equipment = equipment,
        MuscleGroups = muscleGroups
    };
}
