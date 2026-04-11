using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitTracker.Tests.Services;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task GetDailySummaryAsync_AggregatesCompletedWorkoutData()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise("Bench Press", "Strength", "Barbell");

        context.Users.Add(user);
        context.Exercises.Add(exercise);

        var workout = new Workout
        {
            UserId = user.Id,
            Date = DateTime.UtcNow.Date.AddHours(9),
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
                        new Set { SetNumber = 1, Weight = 100m, Reps = 8, RPE = 8 },
                        new Set { SetNumber = 2, Weight = 100m, Reps = 6, RPE = 9 }
                    }
                }
            }
        };

        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);

        var summary = await service.GetDailySummaryAsync(user.Id, DateTime.UtcNow.Date);

        Assert.Equal(1, summary.WorkoutCount);
        Assert.Equal(2, summary.TotalSets);
        Assert.Equal(14, summary.TotalReps);
        Assert.Equal(1400m, summary.TotalVolume);
        Assert.Equal(45, summary.TotalDuration);
        Assert.Contains("Bench Press", summary.ExercisesCompleted);
        Assert.Equal(0, summary.PersonalRecordsAchieved);
    }

    [Fact]
    public async Task GetDailySummaryAsync_CountsPersonalRecordsForTheDay()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise("Deadlift", "Strength", "Barbell");
        var workout = new Workout
        {
            UserId = user.Id,
            Date = DateTime.UtcNow.Date.AddHours(8),
            Duration = 30,
            IsCompleted = true
        };

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Workouts.Add(workout);
        context.PersonalRecords.Add(new PersonalRecord
        {
            UserId = user.Id,
            Exercise = exercise,
            Workout = workout,
            Weight = 225m,
            Reps = 5,
            Date = DateTime.UtcNow.Date.AddHours(8),
            OneRepMax = 262.5m
        });

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        var summary = await service.GetDailySummaryAsync(user.Id, DateTime.UtcNow.Date);

        Assert.Equal(1, summary.PersonalRecordsAchieved);
    }

    [Fact]
    public async Task GetWeeklySummaryAsync_CalculatesFrequencyRestDaysAndVolumeComparison()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise("Squat", "Strength", "Barbell");
        var weekStart = new DateTime(2026, 4, 12);

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, exercise, weekStart.AddDays(1), 100m, 5),
            CreateWorkout(user.Id, exercise, weekStart.AddDays(3), 110m, 5),
            CreateWorkout(user.Id, exercise, weekStart.AddDays(-3), 90m, 5));

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        var summary = await service.GetWeeklySummaryAsync(user.Id, weekStart.AddDays(2));

        Assert.Equal(2, summary.TotalWorkouts);
        Assert.Equal(2, summary.ActiveDays);
        Assert.Equal(5, summary.RestDays);
        Assert.Equal(1050m, summary.TotalVolume);
        Assert.Equal(450m, summary.PreviousPeriodVolume);
        Assert.Equal(2, summary.DailyData.Count(point => point.Workouts == 1));
        Assert.Contains(summary.MuscleGroupDistribution, item => item.MuscleGroup == "Chest");
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_CalculatesAdherenceAndDailyPoints()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise("Row", "Strength", "Cable");
        var month = new DateTime(2026, 4, 1);

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, exercise, month.AddDays(1), 80m, 10),
            CreateWorkout(user.Id, exercise, month.AddDays(10), 90m, 8),
            CreateWorkout(user.Id, exercise, month.AddMonths(-1).AddDays(5), 70m, 8));

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        var summary = await service.GetMonthlySummaryAsync(user.Id, month);

        Assert.Equal(2, summary.TotalWorkouts);
        Assert.Equal(2, summary.ActiveDays);
        Assert.Equal(28, summary.RestDays);
        Assert.Equal(1520m, summary.TotalVolume);
        Assert.Equal(560m, summary.PreviousPeriodVolume);
        Assert.Equal(30, summary.DailyData.Count);
        Assert.True(summary.AdherencePercentage > 6m);
    }

    private static Workout CreateWorkout(string userId, Exercise exercise, DateTime date, decimal weight, int reps) => new()
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
                Sets =
                {
                    new Set { SetNumber = 1, Weight = weight, Reps = reps }
                }
            }
        }
    };

    private static ApplicationUser CreateUser(string id = "user-1") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };

    private static Exercise CreateExercise(string name, string category, string equipment) => new()
    {
        Name = name,
        Category = category,
        Equipment = equipment,
        MuscleGroups = "Chest"
    };
}

public class PersonalRecordServiceTests
{
    [Fact]
    public async Task DetectAndSavePersonalRecordsAsync_CreatesRecordWhenWorkoutBeatsPreviousBest()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = new Exercise { Name = "Bench Press", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Chest" };

        context.Users.Add(user);
        context.Exercises.Add(exercise);

        var previousWorkout = new Workout
        {
            UserId = user.Id,
            Date = DateTime.UtcNow.AddDays(-7),
            Duration = 40,
            IsCompleted = true
        };

        context.Workouts.Add(previousWorkout);
        await context.SaveChangesAsync();

        context.PersonalRecords.Add(new PersonalRecord
        {
            UserId = user.Id,
            ExerciseId = exercise.Id,
            WorkoutId = previousWorkout.Id,
            Weight = 100m,
            Reps = 5,
            Date = previousWorkout.Date,
            OneRepMax = 116.67m
        });

        var workout = new Workout
        {
            UserId = user.Id,
            Date = DateTime.UtcNow,
            Duration = 50,
            IsCompleted = true,
            WorkoutExercises =
            {
                new WorkoutExercise
                {
                    Exercise = exercise,
                    Order = 1,
                    Sets =
                    {
                        new Set { SetNumber = 1, Weight = 105m, Reps = 5 }
                    }
                }
            }
        };

        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        var service = new PersonalRecordService(context);
        var records = await service.DetectAndSavePersonalRecordsAsync(workout);

        var record = Assert.Single(records);
        Assert.Equal(exercise.Id, record.ExerciseId);
        Assert.Equal(105m, record.Weight);
        Assert.Equal(5, record.Reps);
    }

    private static ApplicationUser CreateUser(string id = "user-5") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}

public class ExerciseServiceTests
{
    [Fact]
    public async Task SearchExercisesAsync_AppliesSearchAndFilters()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        context.Exercises.AddRange(
            new Exercise { Name = "Bench Press", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Chest" },
            new Exercise { Name = "Dumbbell Curl", Category = "Strength", Equipment = "Dumbbell", MuscleGroups = "Arms" },
            new Exercise { Name = "Treadmill Run", Category = "Cardio", Equipment = "Treadmill", MuscleGroups = "Legs" });

        await context.SaveChangesAsync();

        var service = new ExerciseService(context);
        var results = await service.SearchExercisesAsync("Bench", "Strength", "Barbell");

        var exercise = Assert.Single(results);
        Assert.Equal("Bench Press", exercise.Name);
    }

    [Fact]
    public async Task GetExerciseHistoryForUserAsync_ReturnsUsageAndBestSet()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = new Exercise { Name = "Squat", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Legs" };

        context.Users.Add(user);
        context.Exercises.Add(exercise);

        context.Workouts.Add(new Workout
        {
            UserId = user.Id,
            Date = DateTime.UtcNow.AddDays(-1),
            Duration = 50,
            IsCompleted = true,
            WorkoutExercises =
            {
                new WorkoutExercise
                {
                    Exercise = exercise,
                    Order = 1,
                    Sets =
                    {
                        new Set { SetNumber = 1, Weight = 185m, Reps = 5 },
                        new Set { SetNumber = 2, Weight = 195m, Reps = 3 }
                    }
                }
            }
        });

        await context.SaveChangesAsync();

        var service = new ExerciseService(context);
        var history = await service.GetExerciseHistoryForUserAsync(exercise.Id, user.Id);

        Assert.Equal(1, history.UsageCount);
        Assert.NotNull(history.LastPerformed);
        Assert.NotNull(history.BestSet);
        Assert.Equal(195m, history.BestSet!.Weight);
        Assert.Equal(3, history.BestSet.Reps);
    }

    private static ApplicationUser CreateUser(string id = "user-2") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}

public class WorkoutServiceTests
{
    [Fact]
    public async Task StartWorkoutAsync_ReturnsExistingIncompleteWorkoutForToday()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var existingWorkout = new Workout
        {
            UserId = user.Id,
            Date = DateTime.UtcNow.Date.AddHours(8),
            IsCompleted = false
        };

        context.Users.Add(user);
        context.Workouts.Add(existingWorkout);
        await context.SaveChangesAsync();

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutAsync(user.Id);

        Assert.Equal(existingWorkout.Id, workout.Id);
    }

    [Fact]
    public async Task GetProgressiveOverloadSuggestionsAsync_SuggestsWeightIncreaseAfterStrongTopSet()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = new Exercise { Name = "Bench Press", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Chest" };

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Workouts.Add(new Workout
        {
            UserId = user.Id,
            Date = DateTime.UtcNow.AddDays(-3),
            Duration = 40,
            IsCompleted = true,
            WorkoutExercises =
            {
                new WorkoutExercise
                {
                    Exercise = exercise,
                    Order = 1,
                    Sets =
                    {
                        new Set { SetNumber = 1, Weight = 100m, Reps = 8 },
                        new Set { SetNumber = 2, Weight = 95m, Reps = 10 }
                    }
                }
            }
        });

        await context.SaveChangesAsync();

        var service = new WorkoutService(context);
        var suggestions = await service.GetProgressiveOverloadSuggestionsAsync(user.Id, new[] { exercise.Id }, "lbs");

        Assert.True(suggestions.ContainsKey(exercise.Id));
        var suggestion = suggestions[exercise.Id];
        Assert.Equal(105m, suggestion.SuggestedWeight);
        Assert.Equal(8, suggestion.SuggestedReps);
        Assert.Contains("105", suggestion.Recommendation);
    }

    [Fact]
    public async Task StartWorkoutFromTemplateAsync_CopiesTemplateExercisesToWorkout()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exerciseOne = new Exercise { Name = "Bench Press", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Chest" };
        var exerciseTwo = new Exercise { Name = "Row", Category = "Strength", Equipment = "Cable", MuscleGroups = "Back" };

        context.Users.Add(user);
        context.Exercises.AddRange(exerciseOne, exerciseTwo);
        context.WorkoutTemplates.Add(new WorkoutTemplate
        {
            UserId = user.Id,
            Name = "Upper Day",
            IsActive = true,
            Exercises =
            {
                new WorkoutTemplateExercise { Exercise = exerciseOne, Order = 1, DefaultSets = 3, DefaultReps = 8 },
                new WorkoutTemplateExercise { Exercise = exerciseTwo, Order = 2, DefaultSets = 3, DefaultReps = 10 }
            }
        });

        await context.SaveChangesAsync();

        var templateId = await context.WorkoutTemplates.Select(t => t.Id).SingleAsync();
        var service = new WorkoutService(context);

        var workout = await service.StartWorkoutFromTemplateAsync(templateId, user.Id);

        Assert.NotNull(workout);
        Assert.Equal(2, workout!.WorkoutExercises.Count);
        Assert.Equal(new[] { exerciseOne.Id, exerciseTwo.Id }, workout.WorkoutExercises.OrderBy(we => we.Order).Select(we => we.ExerciseId).ToArray());
    }

    private static ApplicationUser CreateUser(string id = "user-3") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}

public class TemplateServiceTests
{
    [Fact]
    public async Task SaveTemplateAsync_CreatesTemplateWithOrderedExercises()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exerciseOne = new Exercise { Name = "Squat", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Legs" };
        var exerciseTwo = new Exercise { Name = "Lunge", Category = "Strength", Equipment = "Dumbbell", MuscleGroups = "Legs" };

        context.Users.Add(user);
        context.Exercises.AddRange(exerciseOne, exerciseTwo);
        await context.SaveChangesAsync();

        var service = new TemplateService(context);
        var templateId = await service.SaveTemplateAsync(user.Id, new TemplateEditorModel
        {
            Name = "Leg Day",
            Description = "Lower body focus",
            IsActive = true,
            Exercises =
            {
                new TemplateExerciseEditorModel { ExerciseId = exerciseOne.Id, ExerciseName = exerciseOne.Name, DefaultSets = 4, DefaultReps = 6 },
                new TemplateExerciseEditorModel { ExerciseId = exerciseTwo.Id, ExerciseName = exerciseTwo.Name, DefaultSets = 3, DefaultReps = 10 }
            }
        });

        var template = await context.WorkoutTemplates
            .Include(t => t.Exercises)
            .SingleAsync(t => t.Id == templateId);

        Assert.Equal("Leg Day", template.Name);
        Assert.Equal(2, template.Exercises.Count);
        Assert.Equal(new[] { 1, 2 }, template.Exercises.OrderBy(te => te.Order).Select(te => te.Order).ToArray());
    }

    private static ApplicationUser CreateUser(string id = "user-4") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}
