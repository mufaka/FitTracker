using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IWorkoutService
{
    Task<Workout?> GetWorkoutAsync(int workoutId, string userId);
    Task<Workout> StartWorkoutAsync(string userId);
    Task<Workout?> StartWorkoutFromPlanAsync(int planId, string userId);
    Task<bool> AddExerciseToWorkoutAsync(int workoutId, int exerciseId, string userId);
    Task<bool> LogSetAsync(int workoutExerciseId, string userId, decimal? weight, int? reps, int? rpe, int? durationSeconds = null, decimal? distance = null);
    Task<bool> SetExerciseStatusAsync(int workoutExerciseId, string userId, string status);
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
    private readonly IAchievementService _achievementService;
    private readonly IChallengeService _challengeService;

    public WorkoutService(ApplicationDbContext context)
        : this(context, new PersonalRecordService(context), new AchievementService(context), new ChallengeService(context))
    {
    }

    public WorkoutService(
        ApplicationDbContext context,
        IPersonalRecordService personalRecordService,
        IAchievementService achievementService,
        IChallengeService challengeService)
    {
        _context = context;
        _personalRecordService = personalRecordService;
        _achievementService = achievementService;
        _challengeService = challengeService;
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

    public async Task<Workout?> StartWorkoutFromPlanAsync(int planId, string userId)
    {
        // Ownership, active and not-deleted are all checked before any workout is touched
        // (WDM-22, WDM-SEC-02).
        var plan = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Exercises)
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId && p.IsActive && !p.IsDeleted);

        if (plan == null)
            return null;

        var workout = await StartWorkoutAsync(userId);
        if (workout.WorkoutExercises.Any())
            return workout;

        workout.WorkoutPlanId = plan.Id;

        // Identity and order only. The prescription stays on the plan and is read live for
        // guidance, so editing the plan later changes what this workout shows it was aiming at but
        // can never touch a recorded set (WDM-24, WDM-26).
        var plannedExercises = plan.Exercises
            .OrderBy(pe => pe.Order)
            .Select((pe, index) => new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = pe.ExerciseId,
                Order = index + 1,
                Status = WorkoutExerciseStatuses.Pending
            })
            .ToList();

        _context.WorkoutExercises.AddRange(plannedExercises);
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

    public async Task<bool> LogSetAsync(int workoutExerciseId, string userId, decimal? weight, int? reps, int? rpe, int? durationSeconds = null, decimal? distance = null)
    {
        var workoutExercise = await _context.WorkoutExercises
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .FirstOrDefaultAsync(we => we.Id == workoutExerciseId && we.Workout.UserId == userId && !we.Workout.IsCompleted);

        if (workoutExercise == null)
            return false;

        // The weight arrives in whatever unit the user types in; it is stored canonically.
        // Converting here rather than in the page model means no caller can forget.
        var displayUnit = await DisplayUnits.ForUserAsync(_context, userId);

        // Logging against an exercise already rated — which the full list allows — takes the RPE that
        // rating implies, the same as rating after the fact would have done (WDM-58).
        var implied = rpe is null ? WorkoutExerciseStatuses.ImpliedRpe(workoutExercise.Status) : null;

        _context.Sets.Add(new Set
        {
            WorkoutExerciseId = workoutExerciseId,
            // Past the highest number so far, not the count: removing a set does not renumber the
            // ones left, so counting would hand the new set a number that is already taken.
            SetNumber = workoutExercise.Sets.Count == 0 ? 1 : workoutExercise.Sets.Max(s => s.SetNumber) + 1,
            Weight = UnitConverter.ToCanonicalWeight(weight, displayUnit),
            Reps = reps,
            RPE = rpe ?? implied,
            IsRpeDerived = implied.HasValue,
            Duration = durationSeconds,
            Distance = UnitConverter.ToCanonicalDistance(distance, displayUnit)
        });

        // Recording work against something marked skipped contradicts the mark, so the mark goes
        // rather than the work (WDM-53). Back to Pending, not to an effort rating — only the user
        // says how it felt.
        if (workoutExercise.Status == WorkoutExerciseStatuses.Skipped)
            workoutExercise.Status = WorkoutExerciseStatuses.Pending;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetExerciseStatusAsync(int workoutExerciseId, string userId, string status)
    {
        if (!WorkoutExerciseStatuses.IsKnown(status))
            return false;

        var workoutExercise = await _context.WorkoutExercises
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .FirstOrDefaultAsync(we => we.Id == workoutExerciseId && we.Workout.UserId == userId && !we.Workout.IsCompleted);

        if (workoutExercise == null)
            return false;

        // Skipping something that has recorded sets would contradict the record (WDM-53).
        if (status == WorkoutExerciseStatuses.Skipped && workoutExercise.Sets.Count > 0)
            return false;

        workoutExercise.Status = status;

        // The rating stands in for an RPE nobody typed (WDM-58). It only ever writes over a value this
        // same rule wrote — anything the user entered is theirs and survives a re-rating — and a status
        // that implies no effort clears the derived ones back off, so clearing an answer really does
        // undo it rather than leaving a number behind.
        var implied = WorkoutExerciseStatuses.ImpliedRpe(status);
        foreach (var set in workoutExercise.Sets.Where(s => s.RPE is null || s.IsRpeDerived))
        {
            set.RPE = implied;
            set.IsRpeDerived = implied.HasValue;
        }

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

        // Not "has any exercise": starting from a plan creates a row per planned exercise, so that
        // test became vacuous and would let a workout in which nothing was done be completed —
        // feeding personal records, achievements and challenges from it (WDM-54, WDM-56).
        if (!workout.WorkoutExercises.Any(we => we.IsPerformed))
            return WorkoutCompletionResult.Failure("Record a set or rate an exercise before completing the workout");

        var duration = (int)(DateTime.UtcNow - workout.Date).TotalMinutes;

        workout.IsCompleted = true;
        workout.Duration = duration > 0 ? duration : 1;
        workout.Notes = notes;

        await _context.SaveChangesAsync();
        await _personalRecordService.DetectAndSavePersonalRecordsAsync(workout);
        var unlockedAchievements = await _achievementService.EvaluateAndUnlockAchievementsAsync(userId, DateTime.UtcNow);
        await _challengeService.EvaluateChallengesAsync(userId, DateTime.UtcNow);

        return WorkoutCompletionResult.Success(unlockedAchievements.Select(userAchievement => userAchievement.AchievementId).ToList());
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
        var displayUnit = UnitConverter.NormalizeWeightUnit(userUnits);

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
            // then suggest the next weight jump while holding reps steady. The jump is chosen in
            // the user's own unit, because the plates that exist differ between them.
            if (weightedTopSet?.Weight.HasValue == true && weightedTopSet.Reps.HasValue)
            {
                var lastWeight = UnitConverter.ToDisplayWeight(weightedTopSet.Weight.Value, displayUnit);
                var lastReps = weightedTopSet.Reps.Value;

                if (lastReps >= 8)
                {
                    var target = lastWeight + UnitConverter.WeightIncrement(lastWeight, displayUnit);
                    suggestion.SuggestedWeight = UnitConverter.ToCanonicalWeight(target, displayUnit);
                    suggestion.SuggestedReps = lastReps;
                    suggestion.Recommendation = $"Try {target:0.##} {displayUnit} for {suggestion.SuggestedReps} reps on your top set.";
                }
                else
                {
                    suggestion.SuggestedWeight = weightedTopSet.Weight.Value;
                    suggestion.SuggestedReps = lastReps + 1;
                    suggestion.Recommendation = $"Keep {lastWeight:0.##} {displayUnit} on the bar and aim for {suggestion.SuggestedReps} reps.";
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
}

/// <summary>
/// A suggestion for the next time an exercise is performed. Every weight here is canonical, like
/// every other measurement leaving a service; the view converts through <see cref="UnitConverter"/>.
/// <see cref="Recommendation"/> is the one exception — it is already prose, so the service builds it
/// in the user's own unit.
/// </summary>
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

public record WorkoutCompletionResult(bool Succeeded, string? ErrorMessage, IReadOnlyList<int> UnlockedAchievementIds)
{
    public static WorkoutCompletionResult Success(IReadOnlyList<int>? unlockedAchievementIds = null) => new(true, null, unlockedAchievementIds ?? Array.Empty<int>());
    public static WorkoutCompletionResult Failure(string errorMessage) => new(false, errorMessage, Array.Empty<int>());
}
