using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IWorkoutSuggestionService
{
    Task<WorkoutSuggestionSummary> GetSuggestionsAsync(string userId, int recentDays = 28);
}

public class WorkoutSuggestionService : IWorkoutSuggestionService
{
    private readonly ApplicationDbContext _context;

    public WorkoutSuggestionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutSuggestionSummary> GetSuggestionsAsync(string userId, int recentDays = 28)
    {
        var normalizedRecentDays = Math.Max(7, recentDays);
        var periodStart = DateTime.UtcNow.Date.AddDays(-(normalizedRecentDays - 1));
        var recentExerciseCutoff = DateTime.UtcNow.Date.AddDays(-7);

        var allExercises = await _context.Exercises
            .AsNoTracking()
            .OrderBy(exercise => exercise.Category)
            .ThenBy(exercise => exercise.Name)
            .ToListAsync();

        if (!allExercises.Any())
        {
            return new WorkoutSuggestionSummary
            {
                Headline = "No suggestions yet",
                Summary = "Add exercises to the library to generate workout suggestions."
            };
        }

        var recentWorkoutExercises = await _context.WorkoutExercises
            .AsNoTracking()
            .Include(workoutExercise => workoutExercise.Workout)
            .Include(workoutExercise => workoutExercise.Exercise)
            .Where(workoutExercise => workoutExercise.Workout.UserId == userId &&
                                      workoutExercise.Workout.IsCompleted &&
                                      workoutExercise.Workout.Date >= periodStart)
            .ToListAsync();

        var activeTemplates = await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(template => template.Exercises)
                .ThenInclude(templateExercise => templateExercise.Exercise)
            .Where(template => template.UserId == userId && template.IsActive)
            .ToListAsync();

        var allMuscleGroups = allExercises
            .SelectMany(exercise => SplitMuscleGroups(exercise.MuscleGroups))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(muscleGroup => muscleGroup)
            .ToList();

        var recentMuscleGroupUsage = recentWorkoutExercises
            .SelectMany(workoutExercise => SplitMuscleGroups(workoutExercise.Exercise.MuscleGroups).Distinct(StringComparer.OrdinalIgnoreCase))
            .GroupBy(muscleGroup => muscleGroup, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var orderedFocusCandidates = recentWorkoutExercises.Any()
            ? allMuscleGroups
                .OrderBy(muscleGroup => recentMuscleGroupUsage.GetValueOrDefault(muscleGroup, 0))
                .ThenBy(muscleGroup => muscleGroup)
            : allMuscleGroups
                .OrderBy(muscleGroup => muscleGroup);

        var focusMuscleGroups = orderedFocusCandidates
            .Take(Math.Min(2, allMuscleGroups.Count))
            .ToList();

        var recentExerciseUsage = recentWorkoutExercises
            .GroupBy(workoutExercise => workoutExercise.ExerciseId)
            .ToDictionary(group => group.Key, group => group.Count());

        var recentlyPerformedExerciseIds = recentWorkoutExercises
            .Where(workoutExercise => workoutExercise.Workout.Date >= recentExerciseCutoff)
            .Select(workoutExercise => workoutExercise.ExerciseId)
            .ToHashSet();

        var suggestedExerciseCandidates = allExercises
            .Select(exercise => new
            {
                Exercise = exercise,
                FocusMatchCount = CountFocusMatches(exercise.MuscleGroups, focusMuscleGroups),
                RecentUsageCount = recentExerciseUsage.GetValueOrDefault(exercise.Id, 0),
                RecentlyPerformed = recentlyPerformedExerciseIds.Contains(exercise.Id)
            })
            .Where(candidate => candidate.FocusMatchCount > 0)
            .OrderBy(candidate => candidate.RecentlyPerformed)
            .ThenBy(candidate => candidate.RecentUsageCount)
            .ThenByDescending(candidate => candidate.FocusMatchCount)
            .ThenBy(candidate => candidate.Exercise.Name)
            .Take(3)
            .ToList();

        var suggestedExerciseIds = suggestedExerciseCandidates
            .Select(candidate => candidate.Exercise.Id)
            .ToList();

        var lastPerformedByExerciseId = await _context.WorkoutExercises
            .AsNoTracking()
            .Include(workoutExercise => workoutExercise.Workout)
            .Where(workoutExercise => suggestedExerciseIds.Contains(workoutExercise.ExerciseId) &&
                                      workoutExercise.Workout.UserId == userId &&
                                      workoutExercise.Workout.IsCompleted)
            .GroupBy(workoutExercise => workoutExercise.ExerciseId)
            .Select(group => new
            {
                ExerciseId = group.Key,
                LastPerformed = group.Max(workoutExercise => workoutExercise.Workout.Date)
            })
            .ToDictionaryAsync(item => item.ExerciseId, item => (DateTime?)item.LastPerformed);

        var suggestedExercises = suggestedExerciseCandidates
            .Select(candidate => new SuggestedExerciseItem
            {
                ExerciseId = candidate.Exercise.Id,
                Name = candidate.Exercise.Name,
                Category = candidate.Exercise.Category,
                Equipment = candidate.Exercise.Equipment,
                MuscleGroups = candidate.Exercise.MuscleGroups,
                LastPerformed = lastPerformedByExerciseId.GetValueOrDefault(candidate.Exercise.Id),
                Reason = candidate.RecentlyPerformed
                    ? $"Targets {BuildMuscleGroupPhrase(candidate.Exercise.MuscleGroups, focusMuscleGroups)} and fits your current focus."
                    : $"Targets {BuildMuscleGroupPhrase(candidate.Exercise.MuscleGroups, focusMuscleGroups)} and has not shown up recently."
            })
            .ToList();

        var templateSuggestion = activeTemplates
            .Select(template => new
            {
                Template = template,
                FocusMatchCount = template.Exercises.Count(templateExercise => CountFocusMatches(templateExercise.Exercise.MuscleGroups, focusMuscleGroups) > 0),
                RecentUsageScore = template.Exercises.Sum(templateExercise => recentExerciseUsage.GetValueOrDefault(templateExercise.ExerciseId, 0))
            })
            .Where(candidate => !recentWorkoutExercises.Any() || candidate.FocusMatchCount > 0)
            .OrderByDescending(candidate => candidate.FocusMatchCount)
            .ThenBy(candidate => candidate.RecentUsageScore)
            .ThenBy(candidate => candidate.Template.Name)
            .Select(candidate => new SuggestedTemplateItem
            {
                TemplateId = candidate.Template.Id,
                Name = candidate.Template.Name,
                Description = candidate.Template.Description,
                ExerciseCount = candidate.Template.Exercises.Count,
                FocusMuscleGroups = candidate.Template.Exercises
                    .SelectMany(templateExercise => SplitMuscleGroups(templateExercise.Exercise.MuscleGroups))
                    .Where(muscleGroup => focusMuscleGroups.Contains(muscleGroup, StringComparer.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(muscleGroup => muscleGroup)
                    .ToList(),
                Reason = recentWorkoutExercises.Any()
                    ? $"Best match for your least-worked focus: {string.Join(", ", focusMuscleGroups)}."
                    : "A strong starting point based on your saved templates."
            })
            .FirstOrDefault();

        var summary = recentWorkoutExercises.Any()
            ? $"{string.Join(" and ", focusMuscleGroups)} have had the least attention over your last {normalizedRecentDays} days."
            : "Start with a balanced suggestion based on your exercise library and saved templates.";

        return new WorkoutSuggestionSummary
        {
            Headline = "Suggested next session",
            Summary = summary,
            FocusMuscleGroups = focusMuscleGroups,
            SuggestedExercises = suggestedExercises,
            TemplateSuggestion = templateSuggestion
        };
    }

    private static int CountFocusMatches(string muscleGroups, IReadOnlyCollection<string> focusMuscleGroups)
    {
        return SplitMuscleGroups(muscleGroups)
            .Count(muscleGroup => focusMuscleGroups.Contains(muscleGroup, StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildMuscleGroupPhrase(string muscleGroups, IReadOnlyCollection<string> focusMuscleGroups)
    {
        var matches = SplitMuscleGroups(muscleGroups)
            .Where(muscleGroup => focusMuscleGroups.Contains(muscleGroup, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return matches.Any() ? string.Join(", ", matches) : "your current focus";
    }

    private static List<string> SplitMuscleGroups(string muscleGroups)
    {
        return muscleGroups
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(muscleGroup => !string.IsNullOrWhiteSpace(muscleGroup))
            .ToList();
    }
}

public class WorkoutSuggestionSummary
{
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> FocusMuscleGroups { get; set; } = new();
    public List<SuggestedExerciseItem> SuggestedExercises { get; set; } = new();
    public SuggestedTemplateItem? TemplateSuggestion { get; set; }
    public bool HasSuggestions => FocusMuscleGroups.Any() || SuggestedExercises.Any() || TemplateSuggestion != null;
}

public class SuggestedExerciseItem
{
    public int ExerciseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string MuscleGroups { get; set; } = string.Empty;
    public DateTime? LastPerformed { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class SuggestedTemplateItem
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ExerciseCount { get; set; }
    public List<string> FocusMuscleGroups { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}
