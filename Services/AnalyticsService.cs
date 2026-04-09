using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IAnalyticsService
{
    Task<DailySummary> GetDailySummaryAsync(string userId, DateTime date);
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

        // Check for personal records (simplified for now)
        summary.PersonalRecordsAchieved = 0; // TODO: Implement PR detection

        return summary;
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
