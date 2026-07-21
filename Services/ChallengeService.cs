using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IChallengeService
{
    Task<ChallengeOverviewSummary> GetChallengeOverviewAsync(string userId, DateTime? asOf = null);
    Task<List<ChallengeProgressItem>> GetActiveChallengesAsync(string userId, DateTime? asOf = null);
    Task<bool> JoinChallengeAsync(string userId, int challengeId, DateTime? startedDate = null);
    Task<bool> LeaveChallengeAsync(string userId, int challengeId);
    Task<List<UserChallenge>> EvaluateChallengesAsync(string userId, DateTime? asOf = null);
}

/// <summary>
/// Challenges are goals measured over a window that starts when the user joins.
/// Progress is always derived from workout data rather than stored, so editing or
/// deleting a past workout is reflected immediately and there is no counter to
/// keep in sync.
/// </summary>
public class ChallengeService : IChallengeService
{
    private readonly ApplicationDbContext _context;

    public ChallengeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChallengeOverviewSummary> GetChallengeOverviewAsync(string userId, DateTime? asOf = null)
    {
        var today = (asOf ?? DateTime.UtcNow).Date;

        var challenges = await _context.Challenges
            .AsNoTracking()
            .OrderBy(challenge => challenge.Id)
            .ToListAsync();

        var participation = await _context.UserChallenges
            .AsNoTracking()
            .Where(userChallenge => userChallenge.UserId == userId)
            .ToListAsync();

        var participationByChallengeId = participation
            .ToDictionary(userChallenge => userChallenge.ChallengeId);

        var items = new List<ChallengeProgressItem>();
        foreach (var challenge in challenges)
        {
            participationByChallengeId.TryGetValue(challenge.Id, out var joined);
            items.Add(await BuildProgressItemAsync(userId, challenge, joined, today));
        }

        return new ChallengeOverviewSummary { Challenges = items };
    }

    public async Task<List<ChallengeProgressItem>> GetActiveChallengesAsync(string userId, DateTime? asOf = null)
    {
        var overview = await GetChallengeOverviewAsync(userId, asOf);
        return overview.ActiveChallenges;
    }

    public async Task<bool> JoinChallengeAsync(string userId, int challengeId, DateTime? startedDate = null)
    {
        var challengeExists = await _context.Challenges.AnyAsync(challenge => challenge.Id == challengeId);
        if (!challengeExists)
            return false;

        var start = (startedDate ?? DateTime.UtcNow).Date;

        var existing = await _context.UserChallenges
            .FirstOrDefaultAsync(userChallenge => userChallenge.UserId == userId && userChallenge.ChallengeId == challengeId);

        if (existing == null)
        {
            _context.UserChallenges.Add(new UserChallenge
            {
                UserId = userId,
                ChallengeId = challengeId,
                StartedDate = start
            });
        }
        else
        {
            // Re-joining restarts the window rather than creating a second row,
            // which keeps the unique index intact and lets a lapsed attempt be
            // retried.
            existing.StartedDate = start;
            existing.CompletedDate = null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LeaveChallengeAsync(string userId, int challengeId)
    {
        var existing = await _context.UserChallenges
            .FirstOrDefaultAsync(userChallenge => userChallenge.UserId == userId && userChallenge.ChallengeId == challengeId);

        if (existing == null)
            return false;

        _context.UserChallenges.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserChallenge>> EvaluateChallengesAsync(string userId, DateTime? asOf = null)
    {
        var evaluatedAt = asOf ?? DateTime.UtcNow;
        var today = evaluatedAt.Date;

        var joined = await _context.UserChallenges
            .Include(userChallenge => userChallenge.Challenge)
            .Where(userChallenge => userChallenge.UserId == userId && userChallenge.CompletedDate == null)
            .ToListAsync();

        var completed = new List<UserChallenge>();

        foreach (var userChallenge in joined)
        {
            var window = ChallengeWindow.For(
                userChallenge.StartedDate,
                userChallenge.Challenge.DurationDays,
                today);

            // A window that has already closed can no longer be completed.
            if (window.HasExpired)
                continue;

            var currentValue = await MeasureAsync(userId, userChallenge.Challenge.GoalType, window);
            if (currentValue < userChallenge.Challenge.Goal)
                continue;

            userChallenge.CompletedDate = evaluatedAt;
            completed.Add(userChallenge);
        }

        if (completed.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return completed;
    }

    private async Task<ChallengeProgressItem> BuildProgressItemAsync(
        string userId,
        Challenge challenge,
        UserChallenge? joined,
        DateTime today)
    {
        var item = new ChallengeProgressItem
        {
            ChallengeId = challenge.Id,
            Name = challenge.Name,
            Description = challenge.Description,
            Icon = challenge.Icon,
            GoalType = challenge.GoalType,
            TargetValue = challenge.Goal,
            DurationDays = challenge.DurationDays,
            GoalLabel = DescribeGoal(challenge.GoalType, challenge.Goal)
        };

        if (joined == null)
        {
            item.ProgressLabel = $"Runs for {challenge.DurationDays:N0} days once you join.";
            return item;
        }

        var window = ChallengeWindow.For(joined.StartedDate, challenge.DurationDays, today);

        item.HasJoined = true;
        item.StartedDate = window.Start;
        item.EndDate = window.End;
        item.CompletedDate = joined.CompletedDate;
        item.DaysRemaining = window.DaysRemaining;
        item.CurrentValue = await MeasureAsync(userId, challenge.GoalType, window);
        item.ProgressPercentage = challenge.Goal <= 0
            ? 0
            : (int)Math.Min(100, Math.Round(item.CurrentValue / challenge.Goal * 100, MidpointRounding.AwayFromZero));
        item.ProgressLabel = FormatProgress(challenge.GoalType, item.CurrentValue, challenge.Goal);

        return item;
    }

    /// <summary>
    /// Counts only what falls inside the window, which is what separates a
    /// challenge from an achievement — the same workouts stop counting once the
    /// window closes.
    /// </summary>
    private async Task<decimal> MeasureAsync(string userId, string goalType, ChallengeWindow window)
    {
        switch (goalType)
        {
            case AchievementCriteria.CompletedWorkouts:
                return await _context.Workouts
                    .AsNoTracking()
                    .CountAsync(workout => workout.UserId == userId &&
                                           workout.IsCompleted &&
                                           workout.Date >= window.Start &&
                                           workout.Date < window.ExclusiveEnd);

            case AchievementCriteria.TotalSets:
                return await _context.Sets
                    .AsNoTracking()
                    .CountAsync(set => set.WorkoutExercise.Workout.UserId == userId &&
                                       set.WorkoutExercise.Workout.IsCompleted &&
                                       set.WorkoutExercise.Workout.Date >= window.Start &&
                                       set.WorkoutExercise.Workout.Date < window.ExclusiveEnd);

            case AchievementCriteria.TotalVolume:
                return await _context.Sets
                    .AsNoTracking()
                    .Where(set => set.WorkoutExercise.Workout.UserId == userId &&
                                  set.WorkoutExercise.Workout.IsCompleted &&
                                  set.WorkoutExercise.Workout.Date >= window.Start &&
                                  set.WorkoutExercise.Workout.Date < window.ExclusiveEnd)
                    .SumAsync(set => (set.Weight ?? 0) * (set.Reps ?? 0));

            default:
                return 0m;
        }
    }

    private static string DescribeGoal(string goalType, decimal goal) => goalType switch
    {
        AchievementCriteria.CompletedWorkouts => $"{goal:N0} completed workouts",
        AchievementCriteria.TotalSets => $"{goal:N0} total sets",
        AchievementCriteria.TotalVolume => $"{goal:N0} total volume",
        _ => "Goal unavailable"
    };

    private static string FormatProgress(string goalType, decimal currentValue, decimal goal) => goalType switch
    {
        AchievementCriteria.CompletedWorkouts => $"{currentValue:N0} / {goal:N0} completed workouts",
        AchievementCriteria.TotalSets => $"{currentValue:N0} / {goal:N0} total sets",
        AchievementCriteria.TotalVolume => $"{currentValue:N0} / {goal:N0} total volume",
        _ => "Progress unavailable"
    };

    /// <summary>
    /// The inclusive day range a challenge attempt covers. Dates are compared
    /// against an exclusive upper bound so a workout logged at any time on the
    /// final day still counts.
    /// </summary>
    private readonly record struct ChallengeWindow(DateTime Start, DateTime End, DateTime Today)
    {
        /// <summary>
        /// Takes the duration directly rather than reaching through
        /// <see cref="UserChallenge.Challenge"/>, so callers that load
        /// participation rows without an Include cannot trip over a null
        /// navigation property.
        /// </summary>
        public static ChallengeWindow For(DateTime startedDate, int durationDays, DateTime today)
        {
            var start = startedDate.Date;
            var days = Math.Max(1, durationDays);
            return new ChallengeWindow(start, start.AddDays(days - 1), today);
        }

        public DateTime ExclusiveEnd => End.AddDays(1);

        public bool HasExpired => Today > End;

        public int DaysRemaining => Math.Max(0, (End - Today).Days + 1);
    }
}

public class ChallengeOverviewSummary
{
    public List<ChallengeProgressItem> Challenges { get; set; } = new();

    public List<ChallengeProgressItem> ActiveChallenges => Challenges
        .Where(item => item.IsActive)
        .OrderBy(item => item.DaysRemaining)
        .ThenBy(item => item.Name)
        .ToList();

    public List<ChallengeProgressItem> CompletedChallenges => Challenges
        .Where(item => item.IsCompleted)
        .OrderByDescending(item => item.CompletedDate)
        .ThenBy(item => item.Name)
        .ToList();

    public List<ChallengeProgressItem> ExpiredChallenges => Challenges
        .Where(item => item.HasExpired)
        .OrderBy(item => item.Name)
        .ToList();

    public List<ChallengeProgressItem> AvailableChallenges => Challenges
        .Where(item => !item.HasJoined)
        .OrderBy(item => item.Name)
        .ToList();

    public int ActiveCount => ActiveChallenges.Count;
    public int CompletedCount => CompletedChallenges.Count;
    public int AvailableCount => AvailableChallenges.Count;
    public bool HasAny => Challenges.Count > 0;
}

public class ChallengeProgressItem
{
    public int ChallengeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string GoalType { get; set; } = string.Empty;
    public string GoalLabel { get; set; } = string.Empty;
    public int DurationDays { get; set; }

    public bool HasJoined { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int DaysRemaining { get; set; }

    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public int ProgressPercentage { get; set; }
    public string ProgressLabel { get; set; } = string.Empty;

    public bool IsCompleted => CompletedDate != null;

    /// <summary>Joined, still inside the window, and not finished.</summary>
    public bool IsActive => HasJoined && !IsCompleted && DaysRemaining > 0;

    /// <summary>Joined, the window closed, and the goal was never reached.</summary>
    public bool HasExpired => HasJoined && !IsCompleted && DaysRemaining == 0;
}

public static class ChallengeGoalTypes
{
    public const string CompletedWorkouts = AchievementCriteria.CompletedWorkouts;
    public const string TotalSets = AchievementCriteria.TotalSets;
    public const string TotalVolume = AchievementCriteria.TotalVolume;
}
