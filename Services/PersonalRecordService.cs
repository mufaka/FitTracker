using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IPersonalRecordService
{
    Task<List<PersonalRecord>> DetectAndSavePersonalRecordsAsync(Workout workout);
    Task<List<PersonalRecord>> GetRecentRecordsForExerciseAsync(int exerciseId, string userId, int count = 10);
    Task<List<PersonalRecord>> GetRecordsForWorkoutAsync(int workoutId, string userId);
    Task<List<PersonalRecord>> GetRecordsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
}

public class PersonalRecordService : IPersonalRecordService
{
    private readonly ApplicationDbContext _context;

    public PersonalRecordService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PersonalRecord>> DetectAndSavePersonalRecordsAsync(Workout workout)
    {
        var existingRecords = await _context.PersonalRecords
            .Where(pr => pr.WorkoutId == workout.Id && pr.UserId == workout.UserId)
            .ToListAsync();

        if (existingRecords.Any())
            return existingRecords;

        var exerciseIds = workout.WorkoutExercises.Select(we => we.ExerciseId).Distinct().ToList();
        var previousRecords = await _context.PersonalRecords
            .Where(pr => pr.UserId == workout.UserId && exerciseIds.Contains(pr.ExerciseId))
            .ToListAsync();

        var createdRecords = new List<PersonalRecord>();

        foreach (var exerciseGroup in workout.WorkoutExercises.GroupBy(we => we.ExerciseId))
        {
            var candidate = exerciseGroup
                .SelectMany(we => we.Sets)
                .Where(s => s.Reps.HasValue && s.Reps.Value > 0)
                .Select(s => new PersonalRecordCandidate(
                    exerciseGroup.First().ExerciseId,
                    s.Weight ?? 0,
                    s.Reps ?? 0,
                    CalculateOneRepMax(s.Weight ?? 0, s.Reps ?? 0)))
                .OrderByDescending(c => c.OneRepMax)
                .ThenByDescending(c => c.Weight)
                .ThenByDescending(c => c.Reps)
                .FirstOrDefault();

            if (candidate == null)
                continue;

            var previousBest = previousRecords
                .Where(pr => pr.ExerciseId == exerciseGroup.Key)
                .OrderByDescending(pr => pr.OneRepMax)
                .ThenByDescending(pr => pr.Weight)
                .ThenByDescending(pr => pr.Reps)
                .FirstOrDefault();

            if (!IsNewRecord(candidate, previousBest))
                continue;

            createdRecords.Add(new PersonalRecord
            {
                UserId = workout.UserId,
                ExerciseId = candidate.ExerciseId,
                WorkoutId = workout.Id,
                Weight = candidate.Weight,
                Reps = candidate.Reps,
                Date = workout.Date,
                OneRepMax = candidate.OneRepMax
            });
        }

        if (createdRecords.Count > 0)
        {
            _context.PersonalRecords.AddRange(createdRecords);
            await _context.SaveChangesAsync();
        }

        return createdRecords;
    }

    public Task<List<PersonalRecord>> GetRecentRecordsForExerciseAsync(int exerciseId, string userId, int count = 10)
    {
        return _context.PersonalRecords
            .AsNoTracking()
            .Where(pr => pr.ExerciseId == exerciseId && pr.UserId == userId)
            .OrderByDescending(pr => pr.Date)
            .ThenByDescending(pr => pr.OneRepMax)
            .Take(count)
            .ToListAsync();
    }

    public Task<List<PersonalRecord>> GetRecordsForWorkoutAsync(int workoutId, string userId)
    {
        return _context.PersonalRecords
            .AsNoTracking()
            .Include(pr => pr.Exercise)
            .Where(pr => pr.WorkoutId == workoutId && pr.UserId == userId)
            .OrderByDescending(pr => pr.OneRepMax)
            .ThenByDescending(pr => pr.Weight)
            .ToListAsync();
    }

    public Task<List<PersonalRecord>> GetRecordsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.PersonalRecords
            .AsNoTracking()
            .Include(pr => pr.Exercise)
            .Where(pr => pr.UserId == userId)
            .AsQueryable();

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(pr => pr.Date >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);
            query = query.Where(pr => pr.Date < end);
        }

        return query
            .OrderByDescending(pr => pr.Date)
            .ThenByDescending(pr => pr.OneRepMax)
            .ToListAsync();
    }

    private static bool IsNewRecord(PersonalRecordCandidate candidate, PersonalRecord? previousBest)
    {
        if (previousBest == null)
            return true;

        if (candidate.OneRepMax > previousBest.OneRepMax)
            return true;

        if (candidate.OneRepMax < previousBest.OneRepMax)
            return false;

        if (candidate.Weight > previousBest.Weight)
            return true;

        if (candidate.Weight < previousBest.Weight)
            return false;

        return candidate.Reps > previousBest.Reps;
    }

    private static decimal CalculateOneRepMax(decimal weight, int reps)
    {
        if (weight <= 0 || reps <= 0)
            return 0;

        return Math.Round(weight * (1 + (reps / 30m)), 2, MidpointRounding.AwayFromZero);
    }

    private sealed record PersonalRecordCandidate(int ExerciseId, decimal Weight, int Reps, decimal OneRepMax);
}
