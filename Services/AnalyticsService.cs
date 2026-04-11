using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IAnalyticsService
{
    Task<DailySummary> GetDailySummaryAsync(string userId, DateTime date);
    Task<AnalyticsPeriodSummary> GetWeeklySummaryAsync(string userId, DateTime weekDate);
    Task<AnalyticsPeriodSummary> GetMonthlySummaryAsync(string userId, DateTime monthDate);
    Task<decimal> CalculateTotalVolumeAsync(int workoutId);
    Task<int> EstimateCaloriesBurnedAsync(int workoutId);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DailySummary> GetDailySummaryAsync(string userId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var todaysWorkouts = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .Where(w => w.UserId == userId && 
                       w.Date >= dayStart && 
                       w.Date < dayEnd &&
                       w.IsCompleted)
            .ToListAsync();

        var summary = new DailySummary
        {
            Date = date,
            WorkoutCount = todaysWorkouts.Count
        };

        if (!todaysWorkouts.Any())
            return summary;

        // Calculate exercises completed
        summary.ExercisesCompleted = todaysWorkouts
            .SelectMany(w => w.WorkoutExercises)
            .Select(we => we.Exercise.Name)
            .Distinct()
            .ToList();

        // Calculate total duration
        summary.TotalDuration = todaysWorkouts.Sum(w => w.Duration);

        // Calculate total volume (sets × reps × weight)
        var allSets = todaysWorkouts
            .SelectMany(w => w.WorkoutExercises)
            .SelectMany(we => we.Sets)
            .ToList();

        summary.TotalVolume = allSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
        summary.TotalSets = allSets.Count;
        summary.TotalReps = allSets.Sum(s => s.Reps ?? 0);

        // Estimate calories burned
        summary.CaloriesBurned = todaysWorkouts.Sum(w => EstimateCaloriesForWorkout(w));

        summary.PersonalRecordsAchieved = await _context.PersonalRecords
            .AsNoTracking()
            .Where(pr => pr.UserId == userId && pr.Date >= dayStart && pr.Date < dayEnd)
            .CountAsync();

        return summary;
    }

    public Task<AnalyticsPeriodSummary> GetWeeklySummaryAsync(string userId, DateTime weekDate)
    {
        var periodStart = weekDate.Date.AddDays(-(int)weekDate.DayOfWeek);
        var periodEnd = periodStart.AddDays(6);
        var previousStart = periodStart.AddDays(-7);
        var previousEnd = periodStart.AddDays(-1);

        return GetPeriodSummaryAsync(userId, periodStart, periodEnd, $"Week of {periodStart:MMM dd}", previousStart, previousEnd);
    }

    public Task<AnalyticsPeriodSummary> GetMonthlySummaryAsync(string userId, DateTime monthDate)
    {
        var periodStart = new DateTime(monthDate.Year, monthDate.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var previousStart = periodStart.AddMonths(-1);
        var previousEnd = periodStart.AddDays(-1);

        return GetPeriodSummaryAsync(userId, periodStart, periodEnd, periodStart.ToString("MMMM yyyy"), previousStart, previousEnd);
    }

    public async Task<decimal> CalculateTotalVolumeAsync(int workoutId)
    {
        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == workoutId);

        if (workout == null)
            return 0;

        var allSets = workout.WorkoutExercises
            .SelectMany(we => we.Sets)
            .ToList();

        return allSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
    }

    public async Task<int> EstimateCaloriesBurnedAsync(int workoutId)
    {
        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == workoutId);

        if (workout == null)
            return 0;

        return EstimateCaloriesForWorkout(workout);
    }

    private int EstimateCaloriesForWorkout(Workout workout)
    {
        // Simplified calorie estimation
        // Strength training: ~5-7 calories per minute
        // Cardio: ~8-12 calories per minute
        // This is a rough estimate and should be personalized in production

        var strengthMinutes = 0;
        var cardioMinutes = 0;

        var exercises = workout.WorkoutExercises.Select(we => we.Exercise).ToList();
        
        if (exercises.Any(e => e.Category == "Cardio"))
        {
            cardioMinutes = workout.Duration / 2; // Assume half the workout if mixed
            strengthMinutes = workout.Duration - cardioMinutes;
        }
        else
        {
            strengthMinutes = workout.Duration;
        }

        var strengthCalories = strengthMinutes * 6; // Average 6 cal/min
        var cardioCalories = cardioMinutes * 10; // Average 10 cal/min

        return strengthCalories + cardioCalories;
    }

    private async Task<AnalyticsPeriodSummary> GetPeriodSummaryAsync(
        string userId,
        DateTime periodStart,
        DateTime periodEnd,
        string label,
        DateTime previousPeriodStart,
        DateTime previousPeriodEnd)
    {
        var currentPeriodEndExclusive = periodEnd.Date.AddDays(1);
        var previousPeriodEndExclusive = previousPeriodEnd.Date.AddDays(1);

        var workouts = await _context.Workouts
            .AsNoTracking()
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .Where(w => w.UserId == userId &&
                        w.Date >= periodStart &&
                        w.Date < currentPeriodEndExclusive &&
                        w.IsCompleted)
            .ToListAsync();

        var previousPeriodWorkouts = await _context.Workouts
            .AsNoTracking()
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .Where(w => w.UserId == userId &&
                        w.Date >= previousPeriodStart &&
                        w.Date < previousPeriodEndExclusive &&
                        w.IsCompleted)
            .ToListAsync();

        var dailyPoints = Enumerable.Range(0, (periodEnd.Date - periodStart.Date).Days + 1)
            .Select(offset => periodStart.AddDays(offset))
            .Select(day =>
            {
                var dayWorkouts = workouts.Where(w => w.Date.Date == day.Date).ToList();
                var dayVolume = dayWorkouts
                    .SelectMany(w => w.WorkoutExercises)
                    .SelectMany(we => we.Sets)
                    .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));

                return new AnalyticsDataPoint
                {
                    Date = day,
                    Label = (periodEnd.Date - periodStart.Date).Days >= 27 ? day.ToString("MMM dd") : day.ToString("ddd"),
                    Workouts = dayWorkouts.Count,
                    Volume = dayVolume
                };
            })
            .ToList();

        var activeDays = workouts.Select(w => w.Date.Date).Distinct().Count();
        var currentVolume = workouts
            .SelectMany(w => w.WorkoutExercises)
            .SelectMany(we => we.Sets)
            .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));

        var previousVolume = previousPeriodWorkouts
            .SelectMany(w => w.WorkoutExercises)
            .SelectMany(we => we.Sets)
            .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));

        var muscleGroupDistribution = workouts
            .SelectMany(w => w.WorkoutExercises)
            .SelectMany(we => we.Exercise.MuscleGroups.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .DefaultIfEmpty("Other")
                .Select(muscle => new { Muscle = muscle, Volume = we.Sets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0)) }))
            .GroupBy(x => x.Muscle)
            .Select(group => new MuscleGroupDistributionItem
            {
                MuscleGroup = group.Key,
                Volume = group.Sum(x => x.Volume),
                ExerciseCount = group.Count()
            })
            .OrderByDescending(item => item.Volume)
            .ThenBy(item => item.MuscleGroup)
            .ToList();

        var totalDays = (periodEnd.Date - periodStart.Date).Days + 1;

        return new AnalyticsPeriodSummary
        {
            Label = label,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalWorkouts = workouts.Count,
            ActiveDays = activeDays,
            RestDays = Math.Max(0, totalDays - activeDays),
            TotalVolume = currentVolume,
            PreviousPeriodVolume = previousVolume,
            VolumeChangePercentage = CalculateChangePercentage(currentVolume, previousVolume),
            TotalDuration = workouts.Sum(w => w.Duration),
            CaloriesBurned = workouts.Sum(EstimateCaloriesForWorkout),
            WorkoutFrequency = totalDays == 0 ? 0 : Math.Round((decimal)workouts.Count / totalDays, 2),
            AdherencePercentage = totalDays == 0 ? 0 : Math.Round((decimal)activeDays / totalDays * 100, 1),
            DailyData = dailyPoints,
            MuscleGroupDistribution = muscleGroupDistribution
        };
    }

    private static decimal CalculateChangePercentage(decimal currentValue, decimal previousValue)
    {
        if (previousValue == 0)
            return currentValue > 0 ? 100 : 0;

        return Math.Round(((currentValue - previousValue) / previousValue) * 100, 1);
    }
}

public class DailySummary
{
    public DateTime Date { get; set; }
    public int WorkoutCount { get; set; }
    public List<string> ExercisesCompleted { get; set; } = new();
    public decimal TotalVolume { get; set; }
    public int TotalDuration { get; set; }
    public int TotalSets { get; set; }
    public int TotalReps { get; set; }
    public int CaloriesBurned { get; set; }
    public int PersonalRecordsAchieved { get; set; }
}

public class AnalyticsPeriodSummary
{
    public string Label { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalWorkouts { get; set; }
    public decimal WorkoutFrequency { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal PreviousPeriodVolume { get; set; }
    public decimal VolumeChangePercentage { get; set; }
    public int ActiveDays { get; set; }
    public int RestDays { get; set; }
    public int TotalDuration { get; set; }
    public int CaloriesBurned { get; set; }
    public decimal AdherencePercentage { get; set; }
    public List<AnalyticsDataPoint> DailyData { get; set; } = new();
    public List<MuscleGroupDistributionItem> MuscleGroupDistribution { get; set; } = new();
}

public class AnalyticsDataPoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Workouts { get; set; }
    public decimal Volume { get; set; }
}

public class MuscleGroupDistributionItem
{
    public string MuscleGroup { get; set; } = string.Empty;
    public decimal Volume { get; set; }
    public int ExerciseCount { get; set; }
}
