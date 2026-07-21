using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IExerciseService
{
    Task<List<Exercise>> SearchExercisesAsync(string? searchTerm, string? category, string? equipment);
    Task<int> GetTotalExercisesAsync();
    Task<Exercise?> GetExerciseAsync(int id);
    Task<ExerciseHistorySummary> GetExerciseHistoryForUserAsync(int exerciseId, string userId);
}

public class ExerciseService : IExerciseService
{
    private readonly ApplicationDbContext _context;

    public ExerciseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Exercise>> SearchExercisesAsync(string? searchTerm, string? category, string? equipment)
    {
        var query = _context.Exercises.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmedSearchTerm = searchTerm.Trim();
            var pattern = $"%{trimmedSearchTerm}%";

            query = query.Where(e =>
                EF.Functions.Like(e.Name, pattern) ||
                EF.Functions.Like(e.MuscleGroups, pattern) ||
                (e.Description != null && EF.Functions.Like(e.Description, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(equipment))
        {
            var pattern = $"%{equipment.Trim()}%";
            query = query.Where(e => EF.Functions.Like(e.Equipment, pattern));
        }

        return await query
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    public Task<int> GetTotalExercisesAsync()
    {
        return _context.Exercises.AsNoTracking().CountAsync();
    }

    public Task<Exercise?> GetExerciseAsync(int id)
    {
        return _context.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<ExerciseHistorySummary> GetExerciseHistoryForUserAsync(int exerciseId, string userId)
    {
        var workoutExercises = await _context.WorkoutExercises
            .AsNoTracking()
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .Where(we => we.ExerciseId == exerciseId && we.Workout.UserId == userId)
            // "Used 12 times, last on Tuesday" has to mean performed, not merely planned (WDM-55).
            .Where(WorkoutExerciseStatuses.PerformedPredicate)
            .ToListAsync();

        var allSets = workoutExercises.SelectMany(we => we.Sets).ToList();

        return new ExerciseHistorySummary
        {
            UsageCount = workoutExercises.Count,
            LastPerformed = workoutExercises.Any() ? workoutExercises.Max(we => we.Workout.Date) : null,
            BestSet = allSets
                .OrderByDescending(s => s.Weight ?? 0)
                .ThenByDescending(s => s.Reps ?? 0)
                .FirstOrDefault()
        };
    }
}

public class ExerciseHistorySummary
{
    public int UsageCount { get; set; }
    public DateTime? LastPerformed { get; set; }
    public Set? BestSet { get; set; }
}
