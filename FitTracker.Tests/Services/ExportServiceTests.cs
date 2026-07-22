using System.Text;
using System.Text.Json;
using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitTracker.Tests.Services;

public class ExportServiceTests
{
    [Fact]
    public async Task ExportWorkoutsCsvAsync_FiltersByDateAndFlattensSets()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise("Bench Press", "Strength", "Barbell");

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, exercise, new DateTime(2026, 4, 10, 9, 0, 0), 45, (135m, 8), (145m, 6)),
            CreateWorkout(user.Id, exercise, new DateTime(2026, 5, 10, 9, 0, 0), 50, (155m, 5)));

        await context.SaveChangesAsync();

        var service = new ExportService(context);
        var export = await service.ExportWorkoutsCsvAsync(user.Id, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));
        var csv = Encoding.UTF8.GetString(export.Content);

        Assert.Equal("text/csv; charset=utf-8", export.ContentType);
        Assert.Contains("WorkoutId,WorkoutDate,DurationMinutes", csv);
        Assert.Contains("Bench Press", csv);
        Assert.Contains("135", csv);
        Assert.Contains("145", csv);
        Assert.DoesNotContain("155", csv);
        Assert.EndsWith(".csv", export.FileName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportWorkoutsJsonAsync_PreservesNestedWorkoutStructure()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise("Deadlift", "Strength", "Barbell");

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, exercise, new DateTime(2026, 4, 15, 8, 30, 0), 60, (225m, 5)),
            CreateWorkout(user.Id, exercise, new DateTime(2026, 6, 1, 8, 30, 0), 55, (245m, 3)));

        await context.SaveChangesAsync();

        var service = new ExportService(context);
        var export = await service.ExportWorkoutsJsonAsync(user.Id, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));

        using var document = JsonDocument.Parse(export.Content);
        var workouts = document.RootElement;

        Assert.Equal(JsonValueKind.Array, workouts.ValueKind);
        Assert.Single(workouts.EnumerateArray());
        var workout = workouts[0];
        Assert.Equal(60, workout.GetProperty("Duration").GetInt32());
        Assert.Equal("Deadlift", workout.GetProperty("Exercises")[0].GetProperty("Exercise").GetProperty("Name").GetString());
        Assert.Equal(225m, workout.GetProperty("Exercises")[0].GetProperty("Sets")[0].GetProperty("Weight").GetDecimal());
        Assert.Equal("application/json; charset=utf-8", export.ContentType);
    }

    [Fact]
    public async Task ExportMeasurementsCsvAsync_FiltersMeasurementRows()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        context.Users.Add(user);
        context.BodyMeasurements.AddRange(
            new BodyMeasurement { UserId = user.Id, Date = new DateTime(2026, 4, 2), Weight = UnitConverter.ToCanonicalWeight(182m, UnitConverter.Pounds), BodyFatPercentage = 19.5m },
            new BodyMeasurement { UserId = user.Id, Date = new DateTime(2026, 5, 2), Weight = UnitConverter.ToCanonicalWeight(180m, UnitConverter.Pounds), BodyFatPercentage = 19m });

        await context.SaveChangesAsync();

        var service = new ExportService(context);
        var export = await service.ExportMeasurementsCsvAsync(user.Id, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));
        var csv = Encoding.UTF8.GetString(export.Content);

        // The unit belongs in the header, not on every cell, so the file stays parseable.
        Assert.Contains("MeasurementId,Date,Weight (lbs),BodyFatPercentage", csv);
        Assert.Contains("182", csv);
        Assert.DoesNotContain("180", csv);
    }

    [Fact]
    public async Task ExportPersonalRecordsCsvAsync_ExportsExerciseNamesAndOneRepMax()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var exercise = CreateExercise("Squat", "Strength", "Barbell");
        var workout = CreateWorkout(user.Id, exercise, new DateTime(2026, 4, 20, 10, 0, 0), 50, (225m, 5));

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        context.PersonalRecords.Add(new PersonalRecord
        {
            UserId = user.Id,
            ExerciseId = exercise.Id,
            WorkoutId = workout.Id,
            Date = workout.Date,
            Weight = UnitConverter.ToCanonicalWeight(225m, UnitConverter.Pounds),
            Reps = 5,
            OneRepMax = UnitConverter.ToCanonicalWeight(262.5m, UnitConverter.Pounds)
        });
        await context.SaveChangesAsync();

        var service = new ExportService(context);
        var export = await service.ExportPersonalRecordsCsvAsync(user.Id, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));
        var csv = Encoding.UTF8.GetString(export.Content);

        Assert.Contains("PersonalRecordId,Date,Exercise,Weight (lbs),Reps,OneRepMax (lbs),WorkoutId", csv);
        Assert.Contains("Squat", csv);
        Assert.Contains("262.5", csv);
    }

    /// <summary>
    /// The tuple weights read as pounds — the figures a user would type — and are stored canonically.
    /// An export for an lbs user must therefore hand back exactly the numbers passed in here, which
    /// is what makes the assertions above a round-trip test of the conversion as well as of the file.
    /// </summary>
    private static Workout CreateWorkout(string userId, Exercise exercise, DateTime date, int duration, params (decimal Weight, int Reps)[] sets) => new()
    {
        UserId = userId,
        Date = date,
        Duration = duration,
        IsCompleted = true,
        WorkoutExercises =
        {
            new WorkoutExercise
            {
                Exercise = exercise,
                Order = 1,
                Sets = sets.Select((set, index) => new Set
                {
                    SetNumber = index + 1,
                    Weight = UnitConverter.ToCanonicalWeight(set.Weight, UnitConverter.Pounds),
                    Reps = set.Reps
                }).ToList()
            }
        }
    };

    private static ApplicationUser CreateUser(string id = "user-export-1") => new()
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
