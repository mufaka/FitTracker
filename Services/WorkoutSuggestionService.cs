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
            // Everything below counts rows, so the rows have to represent real training. A skipped
            // chest exercise would otherwise raise that muscle group's usage and push it out of the
            // least-worked focus, which is exactly backwards (WDM-55).
            .Where(WorkoutExerciseStatuses.PerformedPredicate)
            .ToListAsync();

        // Plans, not templates: a template can no longer start a workout, so suggesting one would
        // point at a dead end (WDM-28).
        var activePlans = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(plan => plan.Exercises)
                .ThenInclude(planExercise => planExercise.Exercise)
            .Where(plan => plan.UserId == userId && plan.IsActive && !plan.IsDeleted)
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
            // Otherwise "last performed" reports the date of an attempt that was skipped.
            .Where(WorkoutExerciseStatuses.PerformedPredicate)
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

        var planSuggestion = activePlans
            .Select(plan => new
            {
                Plan = plan,
                FocusMatchCount = plan.Exercises.Count(planExercise => CountFocusMatches(planExercise.Exercise.MuscleGroups, focusMuscleGroups) > 0),
                RecentUsageScore = plan.Exercises.Sum(planExercise => recentExerciseUsage.GetValueOrDefault(planExercise.ExerciseId, 0))
            })
            .Where(candidate => !recentWorkoutExercises.Any() || candidate.FocusMatchCount > 0)
            .OrderByDescending(candidate => candidate.FocusMatchCount)
            .ThenBy(candidate => candidate.RecentUsageScore)
            .ThenBy(candidate => candidate.Plan.Name)
            .Select(candidate => new SuggestedPlanItem
            {
                PlanId = candidate.Plan.Id,
                Name = candidate.Plan.Name,
                Description = candidate.Plan.Description,
                ExerciseCount = candidate.Plan.Exercises.Count,
                FocusMuscleGroups = candidate.Plan.Exercises
                    .SelectMany(planExercise => SplitMuscleGroups(planExercise.Exercise.MuscleGroups))
                    .Where(muscleGroup => focusMuscleGroups.Contains(muscleGroup, StringComparer.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(muscleGroup => muscleGroup)
                    .ToList(),
                Reason = recentWorkoutExercises.Any()
                    ? $"Best match for your least-worked focus: {string.Join(", ", focusMuscleGroups)}."
                    : "A strong starting point based on your saved plans."
            })
            .FirstOrDefault();

        var summary = recentWorkoutExercises.Any()
            ? $"{string.Join(" and ", focusMuscleGroups)} have had the least attention over your last {normalizedRecentDays} days."
            : "Start with a balanced suggestion based on your exercise library and saved plans.";

        return new WorkoutSuggestionSummary
        {
            Headline = "Suggested next session",
            Summary = summary,
            FocusMuscleGroups = focusMuscleGroups,
            SuggestedExercises = suggestedExercises,
            PlanSuggestion = planSuggestion
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
    public SuggestedPlanItem? PlanSuggestion { get; set; }
    public bool HasSuggestions => FocusMuscleGroups.Any() || SuggestedExercises.Any() || PlanSuggestion != null;
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

public class SuggestedPlanItem
{
    public int PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ExerciseCount { get; set; }
    public List<string> FocusMuscleGroups { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}
