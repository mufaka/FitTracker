using System.Globalization;
using System.Text;
using System.Text.Json;
using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IExportService
{
    Task<ExportFileResult> ExportWorkoutsCsvAsync(string userId, DateTime? startDate, DateTime? endDate);
    Task<ExportFileResult> ExportWorkoutsJsonAsync(string userId, DateTime? startDate, DateTime? endDate);
    Task<ExportFileResult> ExportMeasurementsCsvAsync(string userId, DateTime? startDate, DateTime? endDate);
    Task<ExportFileResult> ExportPersonalRecordsCsvAsync(string userId, DateTime? startDate, DateTime? endDate);
}

public class ExportService : IExportService
{
    private const string CsvContentType = "text/csv; charset=utf-8";
    private const string JsonContentType = "application/json; charset=utf-8";

    private readonly ApplicationDbContext _context;

    public ExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExportFileResult> ExportWorkoutsCsvAsync(string userId, DateTime? startDate, DateTime? endDate)
    {
        var workouts = await BuildWorkoutQuery(userId, startDate, endDate)
            .Include(workout => workout.WorkoutExercises)
                .ThenInclude(workoutExercise => workoutExercise.Exercise)
            .Include(workout => workout.WorkoutExercises)
                .ThenInclude(workoutExercise => workoutExercise.Sets)
            .OrderBy(workout => workout.Date)
            .ThenBy(workout => workout.Id)
            .ToListAsync();

        var builder = new StringBuilder();
        AppendCsvRow(builder,
            "WorkoutId",
            "WorkoutDate",
            "DurationMinutes",
            "IsCompleted",
            "WorkoutNotes",
            "ExerciseOrder",
            "ExerciseName",
            "ExerciseCategory",
            "ExerciseEquipment",
            "ExerciseNotes",
            "SetNumber",
            "Reps",
            "Weight",
            "SetDurationSeconds",
            "RestTimeSeconds",
            "RPE");

        foreach (var workout in workouts)
        {
            var workoutExercises = workout.WorkoutExercises
                .OrderBy(workoutExercise => workoutExercise.Order)
                .ThenBy(workoutExercise => workoutExercise.Id)
                .ToList();

            if (!workoutExercises.Any())
            {
                AppendCsvRow(builder,
                    workout.Id.ToString(CultureInfo.InvariantCulture),
                    FormatDateTime(workout.Date),
                    workout.Duration.ToString(CultureInfo.InvariantCulture),
                    FormatBoolean(workout.IsCompleted),
                    workout.Notes,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);
                continue;
            }

            foreach (var workoutExercise in workoutExercises)
            {
                var sets = workoutExercise.Sets
                    .OrderBy(set => set.SetNumber)
                    .ThenBy(set => set.Id)
                    .ToList();

                if (!sets.Any())
                {
                    AppendCsvRow(builder,
                        workout.Id.ToString(CultureInfo.InvariantCulture),
                        FormatDateTime(workout.Date),
                        workout.Duration.ToString(CultureInfo.InvariantCulture),
                        FormatBoolean(workout.IsCompleted),
                        workout.Notes,
                        workoutExercise.Order.ToString(CultureInfo.InvariantCulture),
                        workoutExercise.Exercise.Name,
                        workoutExercise.Exercise.Category,
                        workoutExercise.Exercise.Equipment,
                        workoutExercise.Notes,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null);
                    continue;
                }

                foreach (var set in sets)
                {
                    AppendCsvRow(builder,
                        workout.Id.ToString(CultureInfo.InvariantCulture),
                        FormatDateTime(workout.Date),
                        workout.Duration.ToString(CultureInfo.InvariantCulture),
                        FormatBoolean(workout.IsCompleted),
                        workout.Notes,
                        workoutExercise.Order.ToString(CultureInfo.InvariantCulture),
                        workoutExercise.Exercise.Name,
                        workoutExercise.Exercise.Category,
                        workoutExercise.Exercise.Equipment,
                        workoutExercise.Notes,
                        set.SetNumber.ToString(CultureInfo.InvariantCulture),
                        FormatNullableInt(set.Reps),
                        FormatNullableDecimal(set.Weight),
                        FormatNullableInt(set.Duration),
                        FormatNullableInt(set.RestTime),
                        FormatNullableInt(set.RPE));
                }
            }
        }

        return CreateFileResult(builder.ToString(), BuildFileName("workouts", "csv", startDate, endDate), CsvContentType);
    }

    public async Task<ExportFileResult> ExportWorkoutsJsonAsync(string userId, DateTime? startDate, DateTime? endDate)
    {
        var workouts = await BuildWorkoutQuery(userId, startDate, endDate)
            .Include(workout => workout.WorkoutExercises)
                .ThenInclude(workoutExercise => workoutExercise.Exercise)
            .Include(workout => workout.WorkoutExercises)
                .ThenInclude(workoutExercise => workoutExercise.Sets)
            .OrderBy(workout => workout.Date)
            .ThenBy(workout => workout.Id)
            .ToListAsync();

        var payload = workouts.Select(workout => new
        {
            workout.Id,
            workout.Date,
            workout.Duration,
            workout.IsCompleted,
            workout.Notes,
            workout.CreatedAt,
            Exercises = workout.WorkoutExercises
                .OrderBy(workoutExercise => workoutExercise.Order)
                .ThenBy(workoutExercise => workoutExercise.Id)
                .Select(workoutExercise => new
                {
                    workoutExercise.Id,
                    workoutExercise.Order,
                    workoutExercise.Notes,
                    Exercise = new
                    {
                        workoutExercise.ExerciseId,
                        workoutExercise.Exercise.Name,
                        workoutExercise.Exercise.Category,
                        workoutExercise.Exercise.Equipment,
                        workoutExercise.Exercise.MuscleGroups
                    },
                    Sets = workoutExercise.Sets
                        .OrderBy(set => set.SetNumber)
                        .ThenBy(set => set.Id)
                        .Select(set => new
                        {
                            set.Id,
                            set.SetNumber,
                            set.Reps,
                            set.Weight,
                            set.Duration,
                            set.RestTime,
                            set.RPE,
                            set.CreatedAt
                        })
                        .ToList()
                })
                .ToList()
        }).ToList();

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return CreateFileResult(json, BuildFileName("workouts", "json", startDate, endDate), JsonContentType);
    }

    public async Task<ExportFileResult> ExportMeasurementsCsvAsync(string userId, DateTime? startDate, DateTime? endDate)
    {
        var measurements = await BuildMeasurementQuery(userId, startDate, endDate)
            .OrderBy(measurement => measurement.Date)
            .ThenBy(measurement => measurement.Id)
            .ToListAsync();

        var builder = new StringBuilder();
        AppendCsvRow(builder,
            "MeasurementId",
            "Date",
            "Weight",
            "BodyFatPercentage",
            "Chest",
            "Waist",
            "Arms",
            "Legs",
            "Notes");

        foreach (var measurement in measurements)
        {
            AppendCsvRow(builder,
                measurement.Id.ToString(CultureInfo.InvariantCulture),
                FormatDate(measurement.Date),
                FormatNullableDecimal(measurement.Weight),
                FormatNullableDecimal(measurement.BodyFatPercentage),
                FormatNullableDecimal(measurement.Chest),
                FormatNullableDecimal(measurement.Waist),
                FormatNullableDecimal(measurement.Arms),
                FormatNullableDecimal(measurement.Legs),
                measurement.Notes);
        }

        return CreateFileResult(builder.ToString(), BuildFileName("measurements", "csv", startDate, endDate), CsvContentType);
    }

    public async Task<ExportFileResult> ExportPersonalRecordsCsvAsync(string userId, DateTime? startDate, DateTime? endDate)
    {
        var records = await BuildPersonalRecordQuery(userId, startDate, endDate)
            .Include(record => record.Exercise)
            .OrderBy(record => record.Date)
            .ThenBy(record => record.Id)
            .ToListAsync();

        var builder = new StringBuilder();
        AppendCsvRow(builder,
            "PersonalRecordId",
            "Date",
            "Exercise",
            "Weight",
            "Reps",
            "OneRepMax",
            "WorkoutId");

        foreach (var record in records)
        {
            AppendCsvRow(builder,
                record.Id.ToString(CultureInfo.InvariantCulture),
                FormatDateTime(record.Date),
                record.Exercise.Name,
                record.Weight.ToString(CultureInfo.InvariantCulture),
                record.Reps.ToString(CultureInfo.InvariantCulture),
                record.OneRepMax.ToString(CultureInfo.InvariantCulture),
                record.WorkoutId.ToString(CultureInfo.InvariantCulture));
        }

        return CreateFileResult(builder.ToString(), BuildFileName("personal-records", "csv", startDate, endDate), CsvContentType);
    }

    private IQueryable<Workout> BuildWorkoutQuery(string userId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Workouts
            .AsNoTracking()
            .Where(workout => workout.UserId == userId);

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(workout => workout.Date >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);
            query = query.Where(workout => workout.Date < endExclusive);
        }

        return query;
    }

    private IQueryable<BodyMeasurement> BuildMeasurementQuery(string userId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.BodyMeasurements
            .AsNoTracking()
            .Where(measurement => measurement.UserId == userId);

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(measurement => measurement.Date >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);
            query = query.Where(measurement => measurement.Date < endExclusive);
        }

        return query;
    }

    private IQueryable<PersonalRecord> BuildPersonalRecordQuery(string userId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.PersonalRecords
            .AsNoTracking()
            .Where(record => record.UserId == userId);

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(record => record.Date >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);
            query = query.Where(record => record.Date < endExclusive);
        }

        return query;
    }

    private static ExportFileResult CreateFileResult(string content, string fileName, string contentType)
    {
        return new ExportFileResult
        {
            FileName = fileName,
            ContentType = contentType,
            Content = Encoding.UTF8.GetBytes(content)
        };
    }

    private static string BuildFileName(string prefix, string extension, DateTime? startDate, DateTime? endDate)
    {
        var range = startDate.HasValue || endDate.HasValue
            ? $"{(startDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "start")}-{(endDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "latest")}"
            : "all-time";

        return $"fittracker-{prefix}-{range}.{extension}";
    }

    private static void AppendCsvRow(StringBuilder builder, params string?[] values)
    {
        builder.AppendLine(string.Join(',', values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var escaped = value.Replace("\"", "\"\"");
        return escaped.IndexOfAny([',', '\r', '\n', '\"']) >= 0
            ? $"\"{escaped}\""
            : escaped;
    }

    private static string FormatDate(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDateTime(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static string FormatBoolean(bool value) => value ? "true" : "false";

    private static string FormatNullableInt(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatNullableDecimal(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
}

public class ExportFileResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
