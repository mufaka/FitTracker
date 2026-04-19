using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Xunit;

namespace FitTracker.Tests.Services;

public class WorkoutSuggestionServiceTests
{
    [Fact]
    public async Task GetSuggestionsAsync_PrioritizesLeastWorkedMuscleGroupsAndMatchingTemplate()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser();
        var benchPress = CreateExercise("Bench Press", "Strength", "Barbell", "Chest");
        var inclinePress = CreateExercise("Incline Press", "Strength", "Dumbbells", "Chest");
        var squat = CreateExercise("Back Squat", "Strength", "Barbell", "Legs");
        var barbellRow = CreateExercise("Barbell Row", "Strength", "Barbell", "Back");
        var latPulldown = CreateExercise("Lat Pulldown", "Strength", "Cable", "Back");

        context.Users.Add(user);
        context.Exercises.AddRange(benchPress, inclinePress, squat, barbellRow, latPulldown);
        context.Workouts.AddRange(
            CreateWorkout(user.Id, new DateTime(2026, 4, 10), benchPress, inclinePress),
            CreateWorkout(user.Id, new DateTime(2026, 4, 15), squat),
            CreateWorkout(user.Id, new DateTime(2026, 4, 18), benchPress));
        context.WorkoutTemplates.AddRange(
            CreateTemplate(user.Id, "Pull Day", barbellRow, latPulldown),
            CreateTemplate(user.Id, "Push Day", benchPress, inclinePress));

        await context.SaveChangesAsync();

        var service = new WorkoutSuggestionService(context);
        var suggestions = await service.GetSuggestionsAsync(user.Id, 28);

        Assert.Contains("Back", suggestions.FocusMuscleGroups);
        Assert.NotNull(suggestions.TemplateSuggestion);
        Assert.Equal("Pull Day", suggestions.TemplateSuggestion!.Name);
        Assert.Contains(suggestions.SuggestedExercises, exercise => exercise.Name == "Barbell Row");
        Assert.Contains(suggestions.SuggestedExercises, exercise => exercise.Name == "Lat Pulldown");
    }

    [Fact]
    public async Task GetSuggestionsAsync_UsesSavedTemplatesAndLibraryWhenNoHistoryExists()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = CreateUser("user-suggestions-2");
        var squat = CreateExercise("Goblet Squat", "Strength", "Dumbbells", "Legs");
        var row = CreateExercise("Seated Row", "Strength", "Cable", "Back");
        var press = CreateExercise("Shoulder Press", "Strength", "Dumbbells", "Shoulders");

        context.Users.Add(user);
        context.Exercises.AddRange(squat, row, press);
        context.WorkoutTemplates.Add(CreateTemplate(user.Id, "Starter Full Body", squat, row, press));
        await context.SaveChangesAsync();

        var service = new WorkoutSuggestionService(context);
        var suggestions = await service.GetSuggestionsAsync(user.Id, 28);

        Assert.True(suggestions.HasSuggestions);
        Assert.NotNull(suggestions.TemplateSuggestion);
        Assert.Equal("Starter Full Body", suggestions.TemplateSuggestion!.Name);
        Assert.NotEmpty(suggestions.SuggestedExercises);
    }

    private static Workout CreateWorkout(string userId, DateTime date, params Exercise[] exercises) => new()
    {
        UserId = userId,
        Date = date,
        Duration = 45,
        IsCompleted = true,
        WorkoutExercises = exercises
            .Select((exercise, index) => new WorkoutExercise
            {
                Exercise = exercise,
                Order = index + 1,
                Sets =
                {
                    new Set
                    {
                        SetNumber = 1,
                        Weight = 100m + index * 10,
                        Reps = 8
                    }
                }
            })
            .ToList()
    };

    private static WorkoutTemplate CreateTemplate(string userId, string name, params Exercise[] exercises) => new()
    {
        UserId = userId,
        Name = name,
        IsActive = true,
        Exercises = exercises
            .Select((exercise, index) => new WorkoutTemplateExercise
            {
                Exercise = exercise,
                Order = index + 1,
                DefaultSets = 3,
                DefaultReps = 8
            })
            .ToList()
    };

    private static ApplicationUser CreateUser(string id = "user-suggestions-1") => new()
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
