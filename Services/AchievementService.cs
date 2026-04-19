using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IAchievementService
{
    Task<List<UserAchievement>> EvaluateAndUnlockAchievementsAsync(string userId, DateTime? unlockedDate = null);
    Task<AchievementOverviewSummary> GetAchievementOverviewAsync(string userId);
    Task<List<UserAchievement>> GetUnlockedAchievementsAsync(string userId, IEnumerable<int> achievementIds);
}

public class AchievementService : IAchievementService
{
    private readonly ApplicationDbContext _context;

    public AchievementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserAchievement>> EvaluateAndUnlockAchievementsAsync(string userId, DateTime? unlockedDate = null)
    {
        var achievements = await _context.Achievements
            .OrderBy(achievement => achievement.Id)
            .ToListAsync();

        if (!achievements.Any())
            return new List<UserAchievement>();

        var unlockedAchievementIds = await _context.UserAchievements
            .Where(userAchievement => userAchievement.UserId == userId)
            .Select(userAchievement => userAchievement.AchievementId)
            .ToListAsync();

        var unlockedAchievementIdSet = unlockedAchievementIds.ToHashSet();
        var metrics = await GetMetricsAsync(userId);
        var unlockTimestamp = unlockedDate ?? DateTime.UtcNow;
        var createdAchievements = new List<UserAchievement>();

        foreach (var achievement in achievements.Where(achievement => !unlockedAchievementIdSet.Contains(achievement.Id)))
        {
            var evaluation = EvaluateAchievement(achievement.Criteria, metrics);
            if (evaluation.CurrentValue < evaluation.TargetValue)
                continue;

            createdAchievements.Add(new UserAchievement
            {
                UserId = userId,
                AchievementId = achievement.Id,
                Achievement = achievement,
                UnlockedDate = unlockTimestamp
            });
        }

        if (createdAchievements.Count == 0)
            return createdAchievements;

        _context.UserAchievements.AddRange(createdAchievements);
        await _context.SaveChangesAsync();
        return createdAchievements;
    }

    public async Task<AchievementOverviewSummary> GetAchievementOverviewAsync(string userId)
    {
        var achievements = await _context.Achievements
            .AsNoTracking()
            .OrderBy(achievement => achievement.Id)
            .ToListAsync();

        var unlockedAchievements = await _context.UserAchievements
            .AsNoTracking()
            .Include(userAchievement => userAchievement.Achievement)
            .Where(userAchievement => userAchievement.UserId == userId)
            .ToListAsync();

        var metrics = await GetMetricsAsync(userId);
        var unlockedByAchievementId = unlockedAchievements.ToDictionary(userAchievement => userAchievement.AchievementId);

        var items = achievements
            .Select(achievement =>
            {
                unlockedByAchievementId.TryGetValue(achievement.Id, out var unlockedAchievement);
                var evaluation = EvaluateAchievement(achievement.Criteria, metrics);

                return new AchievementProgressItem
                {
                    AchievementId = achievement.Id,
                    Name = achievement.Name,
                    Description = achievement.Description,
                    Icon = achievement.Icon,
                    Criteria = achievement.Criteria,
                    IsUnlocked = unlockedAchievement != null,
                    UnlockedDate = unlockedAchievement?.UnlockedDate,
                    CurrentValue = evaluation.CurrentValue,
                    TargetValue = evaluation.TargetValue,
                    ProgressPercentage = evaluation.ProgressPercentage,
                    ProgressLabel = evaluation.ProgressLabel
                };
            })
            .OrderBy(item => item.IsUnlocked ? 0 : 1)
            .ThenBy(item => item.IsUnlocked ? item.UnlockedDate : null)
            .ThenBy(item => item.Name)
            .ToList();

        return new AchievementOverviewSummary
        {
            Achievements = items
        };
    }

    public Task<List<UserAchievement>> GetUnlockedAchievementsAsync(string userId, IEnumerable<int> achievementIds)
    {
        var achievementIdList = achievementIds.Distinct().ToList();
        if (!achievementIdList.Any())
            return Task.FromResult(new List<UserAchievement>());

        return _context.UserAchievements
            .AsNoTracking()
            .Include(userAchievement => userAchievement.Achievement)
            .Where(userAchievement => userAchievement.UserId == userId && achievementIdList.Contains(userAchievement.AchievementId))
            .OrderBy(userAchievement => userAchievement.UnlockedDate)
            .ThenBy(userAchievement => userAchievement.Achievement.Name)
            .ToListAsync();
    }

    private async Task<AchievementMetrics> GetMetricsAsync(string userId)
    {
        var completedWorkouts = await _context.Workouts
            .AsNoTracking()
            .Where(workout => workout.UserId == userId && workout.IsCompleted)
            .ToListAsync();

        var workoutDates = completedWorkouts
            .Select(workout => workout.Date.Date)
            .Distinct()
            .OrderByDescending(date => date)
            .ToList();

        var totalSets = await _context.Sets
            .AsNoTracking()
            .Where(set => set.WorkoutExercise.Workout.UserId == userId && set.WorkoutExercise.Workout.IsCompleted)
            .CountAsync();

        var totalPersonalRecords = await _context.PersonalRecords
            .AsNoTracking()
            .Where(record => record.UserId == userId)
            .CountAsync();

        var totalVolume = await _context.Sets
            .AsNoTracking()
            .Where(set => set.WorkoutExercise.Workout.UserId == userId && set.WorkoutExercise.Workout.IsCompleted)
            .SumAsync(set => (set.Weight ?? 0) * (set.Reps ?? 0));

        return new AchievementMetrics
        {
            CompletedWorkouts = completedWorkouts.Count,
            CurrentStreak = CalculateStreak(workoutDates),
            TotalSets = totalSets,
            TotalPersonalRecords = totalPersonalRecords,
            TotalVolume = totalVolume
        };
    }

    private static AchievementEvaluation EvaluateAchievement(string criteria, AchievementMetrics metrics)
    {
        var parts = criteria.Split(':', 2, StringSplitOptions.TrimEntries);
        var key = parts[0];
        var targetValue = parts.Length == 2 && decimal.TryParse(parts[1], out var parsedTarget)
            ? parsedTarget
            : 0m;

        var currentValue = key switch
        {
            AchievementCriteria.CompletedWorkouts => metrics.CompletedWorkouts,
            AchievementCriteria.CurrentStreak => metrics.CurrentStreak,
            AchievementCriteria.TotalSets => metrics.TotalSets,
            AchievementCriteria.PersonalRecords => metrics.TotalPersonalRecords,
            AchievementCriteria.TotalVolume => metrics.TotalVolume,
            _ => 0m
        };

        var progressLabel = key switch
        {
            AchievementCriteria.CompletedWorkouts => $"{currentValue:N0} / {targetValue:N0} completed workouts",
            AchievementCriteria.CurrentStreak => $"{currentValue:N0} / {targetValue:N0} streak days",
            AchievementCriteria.TotalSets => $"{currentValue:N0} / {targetValue:N0} total sets",
            AchievementCriteria.PersonalRecords => $"{currentValue:N0} / {targetValue:N0} PRs",
            AchievementCriteria.TotalVolume => $"{currentValue:N0} / {targetValue:N0} total volume",
            _ => "Progress unavailable"
        };

        var progressPercentage = targetValue <= 0
            ? 0
            : (int)Math.Min(100, Math.Round(currentValue / targetValue * 100, MidpointRounding.AwayFromZero));

        return new AchievementEvaluation(currentValue, targetValue, progressPercentage, progressLabel);
    }

    private static int CalculateStreak(IReadOnlyList<DateTime> workoutDates)
    {
        if (workoutDates.Count == 0)
            return 0;

        var streak = 1;
        var currentDate = workoutDates[0];

        for (var index = 1; index < workoutDates.Count; index++)
        {
            var previousDate = workoutDates[index];
            if ((currentDate - previousDate).Days != 1)
                break;

            streak++;
            currentDate = previousDate;
        }

        return streak;
    }

    private sealed class AchievementMetrics
    {
        public int CompletedWorkouts { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalSets { get; set; }
        public int TotalPersonalRecords { get; set; }
        public decimal TotalVolume { get; set; }
    }

    private sealed record AchievementEvaluation(decimal CurrentValue, decimal TargetValue, int ProgressPercentage, string ProgressLabel);
}

public class AchievementOverviewSummary
{
    public List<AchievementProgressItem> Achievements { get; set; } = new();
    public int TotalCount => Achievements.Count;
    public int UnlockedCount => Achievements.Count(item => item.IsUnlocked);
    public int LockedCount => Achievements.Count(item => !item.IsUnlocked);
    public List<AchievementProgressItem> UnlockedAchievements => Achievements.Where(item => item.IsUnlocked).OrderBy(item => item.UnlockedDate).ThenBy(item => item.Name).ToList();
    public List<AchievementProgressItem> LockedAchievements => Achievements.Where(item => !item.IsUnlocked).OrderByDescending(item => item.ProgressPercentage).ThenBy(item => item.Name).ToList();
}

public class AchievementProgressItem
{
    public int AchievementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedDate { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public int ProgressPercentage { get; set; }
    public string ProgressLabel { get; set; } = string.Empty;
}

public static class AchievementCriteria
{
    public const string CompletedWorkouts = "completed-workouts";
    public const string CurrentStreak = "current-streak";
    public const string TotalSets = "total-sets";
    public const string PersonalRecords = "personal-records";
    public const string TotalVolume = "total-volume";
}
