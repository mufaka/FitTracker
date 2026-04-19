using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IAnalyticsService
{
    Task<DailySummary> GetDailySummaryAsync(string userId, DateTime date);
    Task<AnalyticsPeriodSummary> GetWeeklySummaryAsync(string userId, DateTime weekDate);
    Task<AnalyticsPeriodSummary> GetMonthlySummaryAsync(string userId, DateTime monthDate);
    Task<AdvancedAnalyticsSummary> GetAdvancedDashboardAsync(string userId, int days = 84);
    Task<OverallProgressSummary> GetOverallProgressAsync(string userId, int weeks = 12);
    Task<ExerciseProgressSummary?> GetExerciseProgressAsync(string userId, int exerciseId);
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

    public async Task<AdvancedAnalyticsSummary> GetAdvancedDashboardAsync(string userId, int days = 84)
    {
        var normalizedDays = Math.Max(28, days);
        var periodEnd = DateTime.UtcNow.Date;
        var periodStart = periodEnd.AddDays(-(normalizedDays - 1));
        var periodEndExclusive = periodEnd.AddDays(1);

        var workouts = await _context.Workouts
            .AsNoTracking()
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .Where(w => w.UserId == userId &&
                        w.Date >= periodStart &&
                        w.Date < periodEndExclusive &&
                        w.IsCompleted)
            .OrderBy(w => w.Date)
            .ToListAsync();

        var personalRecords = await _context.PersonalRecords
            .AsNoTracking()
            .Include(pr => pr.Exercise)
            .Where(pr => pr.UserId == userId &&
                         pr.Date >= periodStart &&
                         pr.Date < periodEndExclusive)
            .OrderByDescending(pr => pr.Date)
            .ToListAsync();

        var totalVolume = workouts
            .SelectMany(w => w.WorkoutExercises)
            .SelectMany(we => we.Sets)
            .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));

        var muscleGroups = workouts
            .SelectMany(w => w.WorkoutExercises.Select(we => new { Workout = w, WorkoutExercise = we }))
            .SelectMany(item => item.WorkoutExercise.Exercise.MuscleGroups.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .DefaultIfEmpty("Other")
                .Select(muscle => new
                {
                    MuscleGroup = muscle,
                    Volume = item.WorkoutExercise.Sets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0)),
                    Duration = item.Workout.Duration,
                    WorkoutId = item.Workout.Id
                }))
            .GroupBy(x => x.MuscleGroup)
            .Select(group => new AdvancedMuscleGroupItem
            {
                MuscleGroup = group.Key,
                Volume = group.Sum(x => x.Volume),
                WorkoutCount = group.Select(x => x.WorkoutId).Distinct().Count(),
                AverageDuration = group.Any() ? decimal.Round(group.Average(x => (decimal)x.Duration), 1) : 0m
            })
            .ToList();

        var volumeTrend = Enumerable.Range(0, normalizedDays / 7 + (normalizedDays % 7 == 0 ? 0 : 1))
            .Select(offset => periodStart.AddDays(offset * 7))
            .Where(weekStart => weekStart <= periodEnd)
            .Select(weekStart =>
            {
                var weekEndExclusive = weekStart.AddDays(7);
                var weekWorkouts = workouts.Where(w => w.Date >= weekStart && w.Date < weekEndExclusive).ToList();

                return new AdvancedVolumeTrendPoint
                {
                    PeriodStart = weekStart,
                    Label = weekStart.ToString("MMM dd"),
                    Volume = weekWorkouts
                        .SelectMany(w => w.WorkoutExercises)
                        .SelectMany(we => we.Sets)
                        .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0)),
                    WorkoutCount = weekWorkouts.Count,
                    AverageDuration = weekWorkouts.Any() ? decimal.Round(weekWorkouts.Average(w => (decimal)w.Duration), 1) : 0m
                };
            })
            .ToList();

        var timelineStart = new DateTime(periodEnd.Year, periodEnd.Month, 1).AddMonths(-5);
        var personalRecordTimeline = Enumerable.Range(0, 6)
            .Select(offset => timelineStart.AddMonths(offset))
            .Select(monthStart =>
            {
                var monthEndExclusive = monthStart.AddMonths(1);
                var monthRecords = personalRecords
                    .Where(pr => pr.Date >= monthStart && pr.Date < monthEndExclusive)
                    .ToList();

                return new PersonalRecordTimelinePoint
                {
                    PeriodStart = monthStart,
                    Label = monthStart.ToString("MMM"),
                    RecordCount = monthRecords.Count,
                    BestOneRepMax = monthRecords.Any() ? monthRecords.Max(pr => pr.OneRepMax) : 0
                };
            })
            .ToList();

        var consistencyHeatmap = Enumerable.Range(0, normalizedDays)
            .Select(offset => periodStart.AddDays(offset))
            .Select(day =>
            {
                var dayWorkouts = workouts.Where(w => w.Date.Date == day.Date).ToList();

                return new WorkoutHeatmapCell
                {
                    Date = day,
                    Label = day.ToString("MMM dd"),
                    WorkoutCount = dayWorkouts.Count,
                    Volume = dayWorkouts
                        .SelectMany(w => w.WorkoutExercises)
                        .SelectMany(we => we.Sets)
                        .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                };
            })
            .ToList();

        return new AdvancedAnalyticsSummary
        {
            RangeStart = periodStart,
            RangeEnd = periodEnd,
            TotalWorkouts = workouts.Count,
            TotalVolume = totalVolume,
            AverageWorkoutDuration = workouts.Any() ? decimal.Round(workouts.Average(w => (decimal)w.Duration), 1) : 0m,
            AverageVolumePerWorkout = workouts.Any() ? Math.Round(totalVolume / workouts.Count, 1) : 0,
            TotalPersonalRecords = personalRecords.Count,
            ActiveDays = workouts.Select(w => w.Date.Date).Distinct().Count(),
            MostWorkedMuscleGroups = muscleGroups
                .OrderByDescending(item => item.Volume)
                .ThenBy(item => item.MuscleGroup)
                .Take(5)
                .ToList(),
            LeastWorkedMuscleGroups = muscleGroups
                .OrderBy(item => item.Volume)
                .ThenBy(item => item.MuscleGroup)
                .Take(5)
                .ToList(),
            VolumeTrend = volumeTrend,
            PersonalRecordTimeline = personalRecordTimeline,
            ConsistencyHeatmap = consistencyHeatmap,
            RecentPersonalRecords = personalRecords.Take(8).ToList()
        };
    }

    public async Task<OverallProgressSummary> GetOverallProgressAsync(string userId, int weeks = 12)
    {
        var normalizedWeeks = Math.Max(1, weeks);
        var currentWeekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var periodStart = currentWeekStart.AddDays(-7 * (normalizedWeeks - 1));
        var periodEndExclusive = currentWeekStart.AddDays(7);

        var workouts = await _context.Workouts
            .AsNoTracking()
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .Where(w => w.UserId == userId &&
                        w.Date >= periodStart &&
                        w.Date < periodEndExclusive &&
                        w.IsCompleted)
            .ToListAsync();

        var bodyMeasurements = await _context.BodyMeasurements
            .AsNoTracking()
            .Where(m => m.UserId == userId &&
                        m.Date >= periodStart &&
                        m.Date < periodEndExclusive &&
                        m.Weight.HasValue)
            .OrderBy(m => m.Date)
            .ToListAsync();

        var previousMeasurement = await _context.BodyMeasurements
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date < periodStart && m.Weight.HasValue)
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync();

        var weeklyData = Enumerable.Range(0, normalizedWeeks)
            .Select(offset => periodStart.AddDays(offset * 7))
            .Select(weekStart =>
            {
                var weekEndExclusive = weekStart.AddDays(7);
                var weeklyWorkouts = workouts
                    .Where(w => w.Date >= weekStart && w.Date < weekEndExclusive)
                    .ToList();

                return new OverallProgressPoint
                {
                    PeriodStart = weekStart,
                    Label = weekStart.ToString("MMM dd"),
                    Workouts = weeklyWorkouts.Count,
                    Volume = weeklyWorkouts
                        .SelectMany(w => w.WorkoutExercises)
                        .SelectMany(we => we.Sets)
                        .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                };
            })
            .ToList();

        var firstWeight = bodyMeasurements.FirstOrDefault()?.Weight ?? previousMeasurement?.Weight;
        var latestWeight = bodyMeasurements.LastOrDefault()?.Weight;
        var bodyWeightData = bodyMeasurements
            .Select(m => new MeasurementDataPoint
            {
                Date = m.Date,
                Label = m.Date.ToString("MMM dd"),
                Value = m.Weight!.Value
            })
            .ToList();

        return new OverallProgressSummary
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEndExclusive.AddDays(-1),
            TotalWorkouts = weeklyData.Sum(point => point.Workouts),
            TotalVolume = weeklyData.Sum(point => point.Volume),
            ActiveWeeks = weeklyData.Count(point => point.Workouts > 0),
            AverageWeeklyVolume = Math.Round(weeklyData.Average(point => point.Volume), 1),
            WeeklyData = weeklyData,
            LatestBodyWeight = latestWeight,
            BodyWeightChange = latestWeight.HasValue && firstWeight.HasValue
                ? Math.Round(latestWeight.Value - firstWeight.Value, 2)
                : null,
            BodyWeightData = bodyWeightData
        };
    }

    public async Task<ExerciseProgressSummary?> GetExerciseProgressAsync(string userId, int exerciseId)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
            return null;

        var workoutExercises = await _context.WorkoutExercises
            .AsNoTracking()
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .Where(we => we.ExerciseId == exerciseId &&
                         we.Workout.UserId == userId &&
                         we.Workout.IsCompleted)
            .ToListAsync();

        var personalRecords = await _context.PersonalRecords
            .AsNoTracking()
            .Where(pr => pr.UserId == userId && pr.ExerciseId == exerciseId)
            .ToListAsync();

        var personalRecordsByWorkout = personalRecords
            .GroupBy(pr => pr.WorkoutId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(pr => pr.OneRepMax)
                    .ThenByDescending(pr => pr.Weight)
                    .First());

        var progressPoints = workoutExercises
            .GroupBy(we => new { we.WorkoutId, we.Workout.Date })
            .OrderBy(group => group.Key.Date)
            .Select(group =>
            {
                var allSets = group.SelectMany(we => we.Sets).ToList();
                var bestSet = allSets
                    .OrderByDescending(s => s.Weight ?? 0)
                    .ThenByDescending(s => s.Reps ?? 0)
                    .FirstOrDefault();

                personalRecordsByWorkout.TryGetValue(group.Key.WorkoutId, out var personalRecord);

                return new ExerciseProgressPoint
                {
                    WorkoutId = group.Key.WorkoutId,
                    Date = group.Key.Date,
                    Label = group.Key.Date.ToString("MMM dd"),
                    BestWeight = bestSet?.Weight ?? 0,
                    BestReps = bestSet?.Reps ?? 0,
                    EstimatedOneRepMax = bestSet != null
                        ? OneRepMaxCalculator.CalculateAverage(bestSet.Weight ?? 0, bestSet.Reps ?? 0)
                        : 0,
                    Volume = allSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0)),
                    HasPersonalRecord = personalRecord != null,
                    PersonalRecordWeight = personalRecord?.Weight,
                    PersonalRecordOneRepMax = personalRecord?.OneRepMax
                };
            })
            .ToList();

        return new ExerciseProgressSummary
        {
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            Category = exercise.Category,
            Equipment = exercise.Equipment,
            MuscleGroups = exercise.MuscleGroups,
            SessionCount = progressPoints.Count,
            PersonalRecordCount = personalRecords.Count,
            TotalVolume = progressPoints.Sum(point => point.Volume),
            BestWeight = progressPoints.Count > 0 ? progressPoints.Max(point => point.BestWeight) : 0,
            BestEstimatedOneRepMax = progressPoints.Count > 0 ? progressPoints.Max(point => point.EstimatedOneRepMax) : 0,
            BestOneRepMax = personalRecords.Count > 0 ? personalRecords.Max(record => record.OneRepMax) : 0,
            LastPerformed = progressPoints.LastOrDefault()?.Date,
            ProgressPoints = progressPoints
        };
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

        var bodyMeasurements = await _context.BodyMeasurements
            .AsNoTracking()
            .Where(m => m.UserId == userId &&
                        m.Date >= periodStart &&
                        m.Date < currentPeriodEndExclusive)
            .OrderBy(m => m.Date)
            .ToListAsync();

        var previousMeasurement = await _context.BodyMeasurements
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date < periodStart)
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync();

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
        var firstWeight = bodyMeasurements.FirstOrDefault(m => m.Weight.HasValue)?.Weight ?? previousMeasurement?.Weight;
        var latestWeight = bodyMeasurements.LastOrDefault(m => m.Weight.HasValue)?.Weight;
        var latestBodyFatPercentage = bodyMeasurements.LastOrDefault(m => m.BodyFatPercentage.HasValue)?.BodyFatPercentage;
        var bodyWeightData = bodyMeasurements
            .Where(m => m.Weight.HasValue)
            .Select(m => new MeasurementDataPoint
            {
                Date = m.Date,
                Label = m.Date.ToString("MMM dd"),
                Value = m.Weight!.Value
            })
            .ToList();

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
            MuscleGroupDistribution = muscleGroupDistribution,
            LatestBodyWeight = latestWeight,
            BodyWeightChange = latestWeight.HasValue && firstWeight.HasValue
                ? Math.Round(latestWeight.Value - firstWeight.Value, 2)
                : null,
            LatestBodyFatPercentage = latestBodyFatPercentage,
            BodyWeightData = bodyWeightData
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
    public decimal? LatestBodyWeight { get; set; }
    public decimal? BodyWeightChange { get; set; }
    public decimal? LatestBodyFatPercentage { get; set; }
    public List<MeasurementDataPoint> BodyWeightData { get; set; } = new();
}

public class MeasurementDataPoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class AdvancedAnalyticsSummary
{
    public DateTime RangeStart { get; set; }
    public DateTime RangeEnd { get; set; }
    public int TotalWorkouts { get; set; }
    public int ActiveDays { get; set; }
    public int TotalPersonalRecords { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageWorkoutDuration { get; set; }
    public decimal AverageVolumePerWorkout { get; set; }
    public List<AdvancedMuscleGroupItem> MostWorkedMuscleGroups { get; set; } = new();
    public List<AdvancedMuscleGroupItem> LeastWorkedMuscleGroups { get; set; } = new();
    public List<AdvancedVolumeTrendPoint> VolumeTrend { get; set; } = new();
    public List<PersonalRecordTimelinePoint> PersonalRecordTimeline { get; set; } = new();
    public List<WorkoutHeatmapCell> ConsistencyHeatmap { get; set; } = new();
    public List<PersonalRecord> RecentPersonalRecords { get; set; } = new();
}

public class AdvancedMuscleGroupItem
{
    public string MuscleGroup { get; set; } = string.Empty;
    public decimal Volume { get; set; }
    public int WorkoutCount { get; set; }
    public decimal AverageDuration { get; set; }
}

public class AdvancedVolumeTrendPoint
{
    public DateTime PeriodStart { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Volume { get; set; }
    public int WorkoutCount { get; set; }
    public decimal AverageDuration { get; set; }
}

public class PersonalRecordTimelinePoint
{
    public DateTime PeriodStart { get; set; }
    public string Label { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public decimal BestOneRepMax { get; set; }
}

public class WorkoutHeatmapCell
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int WorkoutCount { get; set; }
    public decimal Volume { get; set; }
}

public class OverallProgressSummary
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalWorkouts { get; set; }
    public int ActiveWeeks { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageWeeklyVolume { get; set; }
    public List<OverallProgressPoint> WeeklyData { get; set; } = new();
    public decimal? LatestBodyWeight { get; set; }
    public decimal? BodyWeightChange { get; set; }
    public List<MeasurementDataPoint> BodyWeightData { get; set; } = new();
}

public class OverallProgressPoint
{
    public DateTime PeriodStart { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Workouts { get; set; }
    public decimal Volume { get; set; }
}

public class ExerciseProgressSummary
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string MuscleGroups { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public int PersonalRecordCount { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal BestWeight { get; set; }
    public decimal BestEstimatedOneRepMax { get; set; }
    public decimal BestOneRepMax { get; set; }
    public DateTime? LastPerformed { get; set; }
    public List<ExerciseProgressPoint> ProgressPoints { get; set; } = new();
}

public class ExerciseProgressPoint
{
    public int WorkoutId { get; set; }
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal BestWeight { get; set; }
    public int BestReps { get; set; }
    public decimal EstimatedOneRepMax { get; set; }
    public decimal Volume { get; set; }
    public bool HasPersonalRecord { get; set; }
    public decimal? PersonalRecordWeight { get; set; }
    public decimal? PersonalRecordOneRepMax { get; set; }
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
