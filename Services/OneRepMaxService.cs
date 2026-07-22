using FitTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IOneRepMaxService
{
    Task<OneRepMaxLeaderboard> GetLeaderboardAsync(string userId, DateTime? asOf = null);
    Task<OneRepMaxTrend?> GetExerciseTrendAsync(string userId, int exerciseId, DateTime? asOf = null);
}

/// <summary>
/// One-rep max history, trend projection and the personal leaderboard.
///
/// Nothing here is stored. Every figure is recomputed from the logged sets on each read, the same
/// way achievements and challenges work, so editing or deleting a past workout is reflected
/// immediately. Only exercises flagged <c>TracksOneRepMax</c> are considered, and only sets whose
/// weight and reps pass <see cref="OneRepMaxCalculator.IsEligible"/>.
/// </summary>
public class OneRepMaxService : IOneRepMaxService
{
    /// <summary>How far back the leaderboard's "recent change" column looks.</summary>
    public const int TrendWindowDays = 90;

    /// <summary>Sessions needed before a projection is offered at all.</summary>
    private const int MinimumProjectionSessions = 3;

    /// <summary>Days the sessions must span, so a single heavy week cannot imply a year of gains.</summary>
    private const int MinimumProjectionSpanDays = 14;

    private static readonly int[] ProjectionHorizons = [30, 60, 90];

    private readonly ApplicationDbContext _context;

    public OneRepMaxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OneRepMaxLeaderboard> GetLeaderboardAsync(string userId, DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;
        var sets = await QueryEligibleSetsAsync(userId, exerciseId: null);

        var entries = sets
            .GroupBy(set => set.ExerciseId)
            .Select(exercise => BuildLeaderboardEntry(exercise.ToList(), now))
            .OrderByDescending(entry => entry.BestOneRepMax)
            .ThenBy(entry => entry.ExerciseName)
            .ToList();

        for (var index = 0; index < entries.Count; index++)
        {
            entries[index].Rank = index + 1;
        }

        return new OneRepMaxLeaderboard
        {
            GeneratedOn = now,
            TrendWindowDays = TrendWindowDays,
            Entries = entries
        };
    }

    public async Task<OneRepMaxTrend?> GetExerciseTrendAsync(string userId, int exerciseId, DateTime? asOf = null)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .Where(e => e.Id == exerciseId && e.TracksOneRepMax)
            .Select(e => new { e.Id, e.Name, e.Category, e.Equipment, e.MuscleGroups })
            .FirstOrDefaultAsync();

        if (exercise == null)
            return null;

        var now = asOf ?? DateTime.UtcNow;
        var sets = await QueryEligibleSetsAsync(userId, exerciseId);
        var points = BuildPoints(sets);

        return new OneRepMaxTrend
        {
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            Category = exercise.Category,
            Equipment = exercise.Equipment,
            MuscleGroups = exercise.MuscleGroups,
            Points = points,
            BestOneRepMax = points.Count > 0 ? points.Max(point => point.OneRepMax) : 0,
            BestAchievedOn = points.OrderByDescending(point => point.OneRepMax).FirstOrDefault()?.Date,
            CurrentOneRepMax = points.LastOrDefault()?.OneRepMax ?? 0,
            LastPerformed = points.LastOrDefault()?.Date,
            Projection = Project(points, now)
        };
    }

    /// <summary>
    /// Every set that could produce a 1RM, scoped to the user inside the query. The rep bounds are
    /// applied in SQL so a long training history does not have to be pulled into memory to be filtered.
    /// </summary>
    private Task<List<EligibleSet>> QueryEligibleSetsAsync(string userId, int? exerciseId)
    {
        var query = _context.Sets
            .AsNoTracking()
            .Where(s => s.WorkoutExercise.Workout.UserId == userId
                        && s.WorkoutExercise.Workout.IsCompleted
                        && s.WorkoutExercise.Exercise.TracksOneRepMax
                        && s.Weight != null
                        && s.Weight > 0
                        && s.Reps != null
                        && (s.Reps == 1
                            || (s.Reps >= OneRepMaxCalculator.MinimumEstimateReps
                                && s.Reps <= OneRepMaxCalculator.MaximumEstimateReps)));

        if (exerciseId.HasValue)
        {
            query = query.Where(s => s.WorkoutExercise.ExerciseId == exerciseId.Value);
        }

        return query
            .Select(s => new EligibleSet
            {
                ExerciseId = s.WorkoutExercise.ExerciseId,
                ExerciseName = s.WorkoutExercise.Exercise.Name,
                Category = s.WorkoutExercise.Exercise.Category,
                Equipment = s.WorkoutExercise.Exercise.Equipment,
                MuscleGroups = s.WorkoutExercise.Exercise.MuscleGroups,
                WorkoutId = s.WorkoutExercise.WorkoutId,
                Date = s.WorkoutExercise.Workout.Date,
                Weight = s.Weight!.Value,
                Reps = s.Reps!.Value
            })
            .ToListAsync();
    }

    /// <summary>
    /// One point per session — the best estimate that session produced — ordered oldest first, with
    /// each point marked if it beat everything before it.
    /// </summary>
    private static List<OneRepMaxPoint> BuildPoints(List<EligibleSet> sets)
    {
        var points = sets
            .GroupBy(set => new { set.WorkoutId, set.Date })
            .Select(session =>
            {
                var best = session
                    .Select(set => new
                    {
                        set.Weight,
                        set.Reps,
                        Estimate = OneRepMaxCalculator.Calculate(set.Weight, set.Reps)
                    })
                    .OrderByDescending(candidate => candidate.Estimate.Average)
                    .ThenByDescending(candidate => candidate.Weight)
                    .First();

                return new OneRepMaxPoint
                {
                    WorkoutId = session.Key.WorkoutId,
                    Date = session.Key.Date,
                    Label = session.Key.Date.ToString("MMM dd"),
                    Weight = best.Weight,
                    Reps = best.Reps,
                    Epley = best.Estimate.Epley,
                    Brzycki = best.Estimate.Brzycki,
                    Lombardi = best.Estimate.Lombardi,
                    OneRepMax = best.Estimate.Average
                };
            })
            .OrderBy(point => point.Date)
            .ThenBy(point => point.WorkoutId)
            .ToList();

        decimal runningBest = 0;
        foreach (var point in points)
        {
            if (point.OneRepMax > runningBest)
            {
                point.IsPersonalBest = true;
                runningBest = point.OneRepMax;
            }
        }

        return points;
    }

    private static OneRepMaxLeaderboardEntry BuildLeaderboardEntry(List<EligibleSet> sets, DateTime now)
    {
        var points = BuildPoints(sets);
        var best = points.OrderByDescending(point => point.OneRepMax).First();
        var latest = points[^1];

        var windowStart = now.Date.AddDays(-TrendWindowDays);
        var windowPoints = points.Where(point => point.Date >= windowStart).ToList();

        return new OneRepMaxLeaderboardEntry
        {
            ExerciseId = sets[0].ExerciseId,
            ExerciseName = sets[0].ExerciseName,
            Category = sets[0].Category,
            Equipment = sets[0].Equipment,
            MuscleGroups = sets[0].MuscleGroups,
            SessionCount = points.Count,
            BestOneRepMax = best.OneRepMax,
            BestWeight = best.Weight,
            BestReps = best.Reps,
            BestAchievedOn = best.Date,
            CurrentOneRepMax = latest.OneRepMax,
            LastPerformed = latest.Date,
            RecentChange = windowPoints.Count >= 2
                ? windowPoints[^1].OneRepMax - windowPoints[0].OneRepMax
                : null
        };
    }

    /// <summary>
    /// Least-squares fit of 1RM against elapsed days, extended forward. Deliberately withheld until
    /// there are enough sessions spread over enough time to be worth extrapolating: a straight line
    /// through two points a week apart would predict gains nobody makes.
    /// </summary>
    private static OneRepMaxProjection? Project(List<OneRepMaxPoint> points, DateTime now)
    {
        if (points.Count < MinimumProjectionSessions)
            return null;

        var origin = points[0].Date.Date;
        var spanDays = (points[^1].Date.Date - origin).TotalDays;
        if (spanDays < MinimumProjectionSpanDays)
            return null;

        var days = points.Select(point => (point.Date.Date - origin).TotalDays).ToArray();
        var values = points.Select(point => (double)point.OneRepMax).ToArray();

        var meanDay = days.Average();
        var meanValue = values.Average();

        var covariance = 0d;
        var variance = 0d;
        for (var index = 0; index < days.Length; index++)
        {
            var dayOffset = days[index] - meanDay;
            covariance += dayOffset * (values[index] - meanValue);
            variance += dayOffset * dayOffset;
        }

        if (variance <= 0)
            return null;

        var slope = covariance / variance;
        var intercept = meanValue - (slope * meanDay);

        var totalSquares = values.Sum(value => Math.Pow(value - meanValue, 2));
        var residualSquares = 0d;
        for (var index = 0; index < days.Length; index++)
        {
            var predicted = intercept + (slope * days[index]);
            residualSquares += Math.Pow(values[index] - predicted, 2);
        }

        var rSquared = totalSquares <= 0 ? 0 : 1 - (residualSquares / totalSquares);

        // Project from today rather than from the last session, so a lapse in training does not
        // silently shift the horizons forward.
        var elapsed = (now.Date - origin).TotalDays;
        var horizons = ProjectionHorizons
            .Select(horizon => new OneRepMaxProjectionPoint
            {
                Days = horizon,
                Date = now.Date.AddDays(horizon),
                Label = now.Date.AddDays(horizon).ToString("MMM dd"),
                OneRepMax = ToWeight(intercept + (slope * (elapsed + horizon)))
            })
            .ToList();

        return new OneRepMaxProjection
        {
            SampleCount = points.Count,
            ChangePerWeek = ToWeight(slope * 7, allowNegative: true),
            RSquared = decimal.Round((decimal)Math.Clamp(rSquared, 0, 1), 2, MidpointRounding.AwayFromZero),
            Horizons = horizons
        };
    }

    private static decimal ToWeight(double value, bool allowNegative = false)
    {
        var rounded = decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
        return allowNegative ? rounded : Math.Max(0, rounded);
    }

    private sealed class EligibleSet
    {
        public int ExerciseId { get; init; }
        public string ExerciseName { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Equipment { get; init; } = string.Empty;
        public string MuscleGroups { get; init; } = string.Empty;
        public int WorkoutId { get; init; }
        public DateTime Date { get; init; }
        public decimal Weight { get; init; }
        public int Reps { get; init; }
    }
}

public class OneRepMaxLeaderboard
{
    public DateTime GeneratedOn { get; set; }
    public int TrendWindowDays { get; set; }
    public List<OneRepMaxLeaderboardEntry> Entries { get; set; } = new();

    public bool HasEntries => Entries.Count > 0;
    public decimal HeaviestOneRepMax => Entries.Count > 0 ? Entries.Max(entry => entry.BestOneRepMax) : 0;
    public int TotalSessions => Entries.Sum(entry => entry.SessionCount);
}

public class OneRepMaxLeaderboardEntry
{
    public int Rank { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string MuscleGroups { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public decimal BestOneRepMax { get; set; }
    public decimal BestWeight { get; set; }
    public int BestReps { get; set; }
    public DateTime BestAchievedOn { get; set; }
    public decimal CurrentOneRepMax { get; set; }
    public DateTime LastPerformed { get; set; }

    /// <summary>Change across the trend window, or null when there are too few sessions in it to compare.</summary>
    public decimal? RecentChange { get; set; }
}

public class OneRepMaxTrend
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string MuscleGroups { get; set; } = string.Empty;
    public List<OneRepMaxPoint> Points { get; set; } = new();
    public decimal BestOneRepMax { get; set; }
    public DateTime? BestAchievedOn { get; set; }
    public decimal CurrentOneRepMax { get; set; }
    public DateTime? LastPerformed { get; set; }
    public OneRepMaxProjection? Projection { get; set; }

    public bool HasHistory => Points.Count > 0;
}

public class OneRepMaxPoint
{
    public int WorkoutId { get; set; }
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Reps { get; set; }
    public decimal Epley { get; set; }
    public decimal Brzycki { get; set; }
    public decimal Lombardi { get; set; }
    public decimal OneRepMax { get; set; }

    /// <summary>Set a new best at the time it was logged.</summary>
    public bool IsPersonalBest { get; set; }
}

public class OneRepMaxProjection
{
    public int SampleCount { get; set; }
    public decimal ChangePerWeek { get; set; }

    /// <summary>How well the sessions fit a straight line, 0 to 1. Low means the trend is noise.</summary>
    public decimal RSquared { get; set; }

    public List<OneRepMaxProjectionPoint> Horizons { get; set; } = new();

    public bool IsImproving => ChangePerWeek > 0;
    public bool IsReliable => RSquared >= 0.5m;
}

public class OneRepMaxProjectionPoint
{
    public int Days { get; set; }
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal OneRepMax { get; set; }
}
