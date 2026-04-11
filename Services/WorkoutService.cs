using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IWorkoutService
{
    Task<Workout?> GetWorkoutAsync(int workoutId, string userId);
    Task<Workout> StartWorkoutAsync(string userId);
    Task<Workout?> StartWorkoutFromTemplateAsync(int templateId, string userId);
    Task<bool> AddExerciseToWorkoutAsync(int workoutId, int exerciseId, string userId);
    Task<bool> LogSetAsync(int workoutExerciseId, string userId, decimal? weight, int? reps, int? rpe);
    Task<bool> RemoveSetAsync(int setId, string userId);
    Task<bool> RemoveExerciseAsync(int workoutExerciseId, string userId);
    Task<WorkoutCompletionResult> CompleteWorkoutAsync(int workoutId, string userId, string? notes);
    Task<bool> CancelWorkoutAsync(int workoutId, string userId);
    Task<decimal> CalculateWorkoutVolumeAsync(int workoutId, string userId);
    Task<Dictionary<int, ProgressiveOverloadSuggestion>> GetProgressiveOverloadSuggestionsAsync(string userId, IEnumerable<int> exerciseIds, string userUnits);
}

public class WorkoutService : IWorkoutService
{
    private readonly ApplicationDbContext _context;
    private readonly IPersonalRecordService _personalRecordService;

    public WorkoutService(ApplicationDbContext context)
        : this(context, new PersonalRecordService(context))
    {
    }

    public WorkoutService(ApplicationDbContext context, IPersonalRecordService personalRecordService)
    {
        _context = context;
        _personalRecordService = personalRecordService;
    }

    public async Task<Workout?> GetWorkoutAsync(int workoutId, string userId)
    {
        return await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.UserId == userId);
    }

    public async Task<Workout> StartWorkoutAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;

        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .Where(w => w.UserId == userId && w.Date.Date == today && !w.IsCompleted)
            .OrderByDescending(w => w.Date)
            .FirstOrDefaultAsync();

        if (workout != null)
            return workout;

        workout = new Workout
        {
            UserId = userId,
            Date = DateTime.UtcNow,
            IsCompleted = false
        };

        _context.Workouts.Add(workout);
        await _context.SaveChangesAsync();

        return workout;
    }

    public async Task<Workout?> StartWorkoutFromTemplateAsync(int templateId, string userId)
    {
        var template = await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises)
            .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId && t.IsActive);

        if (template == null)
            return null;

        var workout = await StartWorkoutAsync(userId);
        if (workout.WorkoutExercises.Any())
            return workout;

        var templateExercises = template.Exercises
            .OrderBy(te => te.Order)
            .Select(te => new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = te.ExerciseId,
                Order = te.Order,
                Notes = te.Notes
            })
            .ToList();

        if (templateExercises.Count == 0)
            return workout;

        _context.WorkoutExercises.AddRange(templateExercises);
        await _context.SaveChangesAsync();

        return await GetWorkoutAsync(workout.Id, userId);
    }

    public async Task<bool> AddExerciseToWorkoutAsync(int workoutId, int exerciseId, string userId)
    {
        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.UserId == userId && !w.IsCompleted);

        if (workout == null)
            return false;

        var exerciseExists = await _context.Exercises.AnyAsync(e => e.Id == exerciseId);
        if (!exerciseExists)
            return false;

        _context.WorkoutExercises.Add(new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exerciseId,
            Order = workout.WorkoutExercises.Count + 1
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LogSetAsync(int workoutExerciseId, string userId, decimal? weight, int? reps, int? rpe)
    {
        var workoutExercise = await _context.WorkoutExercises
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .FirstOrDefaultAsync(we => we.Id == workoutExerciseId && we.Workout.UserId == userId && !we.Workout.IsCompleted);

        if (workoutExercise == null)
            return false;

        _context.Sets.Add(new Set
        {
            WorkoutExerciseId = workoutExerciseId,
            SetNumber = workoutExercise.Sets.Count + 1,
            Weight = weight,
            Reps = reps,
            RPE = rpe
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveSetAsync(int setId, string userId)
    {
        var set = await _context.Sets
            .Include(s => s.WorkoutExercise)
                .ThenInclude(we => we.Workout)
            .FirstOrDefaultAsync(s => s.Id == setId && s.WorkoutExercise.Workout.UserId == userId && !s.WorkoutExercise.Workout.IsCompleted);

        if (set == null)
            return false;

        _context.Sets.Remove(set);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveExerciseAsync(int workoutExerciseId, string userId)
    {
        var workoutExercise = await _context.WorkoutExercises
            .Include(we => we.Workout)
            .FirstOrDefaultAsync(we => we.Id == workoutExerciseId && we.Workout.UserId == userId && !we.Workout.IsCompleted);

        if (workoutExercise == null)
            return false;

        _context.WorkoutExercises.Remove(workoutExercise);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<WorkoutCompletionResult> CompleteWorkoutAsync(int workoutId, string userId, string? notes)
    {
        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.UserId == userId);

        if (workout == null)
            return WorkoutCompletionResult.Failure("Workout not found.");

        if (workout.IsCompleted)
            return WorkoutCompletionResult.Failure("Workout is already completed.");

        if (!workout.WorkoutExercises.Any())
            return WorkoutCompletionResult.Failure("Add at least one exercise before completing the workout");

        var duration = (int)(DateTime.UtcNow - workout.Date).TotalMinutes;

        workout.IsCompleted = true;
        workout.Duration = duration > 0 ? duration : 1;
        workout.Notes = notes;

        await _context.SaveChangesAsync();
        await _personalRecordService.DetectAndSavePersonalRecordsAsync(workout);

        return WorkoutCompletionResult.Success();
    }

    public async Task<bool> CancelWorkoutAsync(int workoutId, string userId)
    {
        var workout = await _context.Workouts
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.UserId == userId);

        if (workout == null)
            return false;

        _context.Workouts.Remove(workout);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> CalculateWorkoutVolumeAsync(int workoutId, string userId)
    {
        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.UserId == userId);

        if (workout == null)
            return 0;

        return workout.WorkoutExercises
            .SelectMany(we => we.Sets)
            .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
    }

    public async Task<Dictionary<int, ProgressiveOverloadSuggestion>> GetProgressiveOverloadSuggestionsAsync(string userId, IEnumerable<int> exerciseIds, string userUnits)
    {
        var exerciseIdList = exerciseIds.Distinct().ToList();
        if (!exerciseIdList.Any())
            return new Dictionary<int, ProgressiveOverloadSuggestion>();

        // Pull the completed history once, then derive the most recent performance per exercise in memory.
        var completedExerciseHistory = await _context.WorkoutExercises
            .AsNoTracking()
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .Where(we => exerciseIdList.Contains(we.ExerciseId) &&
                         we.Workout.UserId == userId &&
                         we.Workout.IsCompleted &&
                         we.Sets.Any())
            .OrderByDescending(we => we.Workout.Date)
            .ToListAsync();

        var suggestions = new Dictionary<int, ProgressiveOverloadSuggestion>();

        foreach (var exerciseId in exerciseIdList)
        {
            var lastPerformance = completedExerciseHistory.FirstOrDefault(we => we.ExerciseId == exerciseId);
            if (lastPerformance == null)
                continue;

            var orderedSets = lastPerformance.Sets
                .OrderBy(s => s.SetNumber)
                .ToList();

            var weightedTopSet = orderedSets
                .Where(s => s.Weight.HasValue && s.Reps.HasValue)
                .OrderByDescending(s => s.Weight)
                .ThenByDescending(s => s.Reps)
                .FirstOrDefault();

            var bestRepSet = orderedSets
                .Where(s => s.Reps.HasValue)
                .OrderByDescending(s => s.Reps)
                .ThenByDescending(s => s.Weight ?? 0)
                .FirstOrDefault();

            var suggestion = new ProgressiveOverloadSuggestion
            {
                LastPerformedOn = lastPerformance.Workout.Date,
                LastSetCount = orderedSets.Count,
                LastTopWeight = weightedTopSet?.Weight,
                LastTopReps = weightedTopSet?.Reps ?? bestRepSet?.Reps,
                LastTotalVolume = orderedSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
            };

            // Use a simple double-progression rule: add reps until the top set is strong enough,
            // then suggest the next weight jump while holding reps steady.
            if (weightedTopSet?.Weight.HasValue == true && weightedTopSet.Reps.HasValue)
            {
                var lastWeight = weightedTopSet.Weight.Value;
                var lastReps = weightedTopSet.Reps.Value;

                if (lastReps >= 8)
                {
                    suggestion.SuggestedWeight = lastWeight + GetSuggestedWeightIncrement(lastWeight);
                    suggestion.SuggestedReps = lastReps;
                    suggestion.Recommendation = $"Try {suggestion.SuggestedWeight:0.##} {userUnits} for {suggestion.SuggestedReps} reps on your top set.";
                }
                else
                {
                    suggestion.SuggestedWeight = lastWeight;
                    suggestion.SuggestedReps = lastReps + 1;
                    suggestion.Recommendation = $"Keep {lastWeight:0.##} {userUnits} on the bar and aim for {suggestion.SuggestedReps} reps.";
                }
            }
            else if (bestRepSet?.Reps.HasValue == true)
            {
                suggestion.SuggestedReps = bestRepSet.Reps.Value + 1;
                suggestion.Recommendation = $"Aim to beat your last best set with {suggestion.SuggestedReps} reps.";
            }
            else
            {
                suggestion.Recommendation = "Repeat the movement and focus on cleaner execution than last time.";
            }

            suggestions[exerciseId] = suggestion;
        }

        return suggestions;
    }

    private static decimal GetSuggestedWeightIncrement(decimal weight)
    {
        return weight < 50 ? 2.5m : 5m;
    }
}

public class ProgressiveOverloadSuggestion
{
    public DateTime LastPerformedOn { get; set; }
    public int LastSetCount { get; set; }
    public int? LastTopReps { get; set; }
    public decimal? LastTopWeight { get; set; }
    public decimal LastTotalVolume { get; set; }
    public int? SuggestedReps { get; set; }
    public decimal? SuggestedWeight { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public record WorkoutCompletionResult(bool Succeeded, string? ErrorMessage)
{
    public static WorkoutCompletionResult Success() => new(true, null);
    public static WorkoutCompletionResult Failure(string errorMessage) => new(false, errorMessage);
}
