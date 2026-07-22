using System.ComponentModel.DataAnnotations;
using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

/// <summary>
/// Which templates a read should return. <see cref="WorkoutTemplate"/> is the only entity with two
/// legitimate read predicates, so the intent is named rather than left as an inline null check
/// duplicated across call sites (WDM-SEC-03). A closed set of strings, following the convention
/// established by <c>AchievementCriteria</c> and <c>ChallengeGoalTypes</c>.
/// </summary>
public static class TemplateOwnership
{
    /// <summary>The caller's own templates together with every built-in.</summary>
    public const string All = "All";

    /// <summary>Only templates the caller owns.</summary>
    public const string Personal = "Personal";

    /// <summary>Only the seeded, ownerless catalog.</summary>
    public const string BuiltIn = "BuiltIn";
}

public interface ITemplateService
{
    Task<List<WorkoutTemplate>> GetTemplatesAsync(string userId, string ownership = TemplateOwnership.All);
    Task<List<WorkoutTemplate>> GetCatalogAsync();
    Task<TemplateEditorModel?> GetTemplateEditorAsync(int templateId, string userId);
    Task<int?> SaveTemplateAsync(string userId, TemplateEditorModel model);
    Task<int?> CopyTemplateAsync(int templateId, string userId);
    Task<bool> DeleteTemplateAsync(int templateId, string userId);
}

public class TemplateService : ITemplateService
{
    private readonly ApplicationDbContext _context;

    public TemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkoutTemplate>> GetTemplatesAsync(string userId, string ownership = TemplateOwnership.All)
    {
        var query = _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises.OrderBy(te => te.Order))
                .ThenInclude(te => te.Exercise)
            .AsQueryable();

        query = ownership switch
        {
            TemplateOwnership.Personal => query.Where(t => t.UserId == userId),
            TemplateOwnership.BuiltIn => query.Where(t => t.UserId == null),
            _ => query.Where(t => t.UserId == userId || t.UserId == null)
        };

        // Built-ins sort after personal templates: a user's own work is what they came for.
        return await query
            .OrderBy(t => t.UserId == null)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<WorkoutTemplate>> GetCatalogAsync()
    {
        // Reachable without a signed-in user (WDM-45), so it must never widen beyond ownerless rows.
        return await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises.OrderBy(te => te.Order))
                .ThenInclude(te => te.Exercise)
            .Where(t => t.UserId == null)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<TemplateEditorModel?> GetTemplateEditorAsync(int templateId, string userId)
    {
        // Deliberately wider than the write predicate: a built-in has to be readable to be previewed
        // and copied. Saving one is still impossible — SaveTemplateAsync keeps the strict predicate.
        var template = await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises.OrderBy(te => te.Order))
                .ThenInclude(te => te.Exercise)
            .FirstOrDefaultAsync(t => t.Id == templateId && (t.UserId == userId || t.UserId == null));

        if (template == null)
            return null;

        return ToEditorModel(template, await DisplayUnits.ForUserAsync(_context, userId));
    }

    public async Task<int?> CopyTemplateAsync(int templateId, string userId)
    {
        var source = await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises.OrderBy(te => te.Order))
            .FirstOrDefaultAsync(t => t.Id == templateId && (t.UserId == userId || t.UserId == null));

        if (source == null)
            return null;

        var copy = new WorkoutTemplate
        {
            UserId = userId,
            Name = BuildCopyName(source.Name),
            Description = source.Description,
            IsActive = true,
            // Never carried over: the catalog key identifies one seeded entry, and a copy is a
            // separate, user-owned template that seeding must not later collide with.
            CatalogKey = null,
            Exercises = source.Exercises
                .OrderBy(te => te.Order)
                .Select((te, index) => new WorkoutTemplateExercise
                {
                    ExerciseId = te.ExerciseId,
                    Order = index + 1,
                    DefaultSets = te.DefaultSets,
                    DefaultReps = te.DefaultReps,
                    DefaultDurationSeconds = te.DefaultDurationSeconds,
                    DefaultDistance = te.DefaultDistance,
                    Notes = te.Notes
                })
                .ToList()
        };

        _context.WorkoutTemplates.Add(copy);
        await _context.SaveChangesAsync();
        return copy.Id;
    }

    /// <summary>
    /// Keeps copies distinguishable without letting the name grow without bound on repeat copies,
    /// and without exceeding the 100-character column.
    /// </summary>
    private static string BuildCopyName(string sourceName)
    {
        const string suffix = " (copy)";
        var name = sourceName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? sourceName
            : sourceName + suffix;

        return name.Length <= 100 ? name : name[..100];
    }

    private static TemplateEditorModel ToEditorModel(WorkoutTemplate template, string displayUnit) => new()
    {
        Id = template.Id,
        Name = template.Name,
        Description = template.Description,
        IsActive = template.IsActive,
        IsBuiltIn = template.IsBuiltIn,
        Exercises = template.Exercises
            .OrderBy(te => te.Order)
            .Select(te => new TemplateExerciseEditorModel
            {
                ExerciseId = te.ExerciseId,
                ExerciseName = te.Exercise.Name,
                Equipment = te.Exercise.Equipment,
                MuscleGroups = te.Exercise.MuscleGroups,
                DefaultSets = te.DefaultSets,
                DefaultReps = te.DefaultReps,
                DefaultDurationSeconds = te.DefaultDurationSeconds,
                // The editor is a form, so distances leave in the unit the user types in.
                DefaultDistance = UnitConverter.ToDisplayDistance(te.DefaultDistance, displayUnit),
                Notes = te.Notes
            })
            .ToList()
    };

    public async Task<int?> SaveTemplateAsync(string userId, TemplateEditorModel model)
    {
        var exerciseIds = model.Exercises.Select(e => e.ExerciseId).Distinct().ToList();
        var existingExerciseIds = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (existingExerciseIds.Count != exerciseIds.Count)
            return null;

        WorkoutTemplate template;
        if (model.Id.HasValue)
        {
            // Strict ownership, unlike the reads above: a built-in has UserId == null and so can
            // never match, which is what makes the catalog immutable through any user-reachable
            // route (WDM-06, WDM-46, WDM-SEC-04).
            var existing = await _context.WorkoutTemplates
                .Include(t => t.Exercises)
                .FirstOrDefaultAsync(t => t.Id == model.Id.Value && t.UserId == userId);

            if (existing == null)
                return null;

            template = existing;
            template.Name = model.Name.Trim();
            template.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            template.IsActive = model.IsActive;

            _context.WorkoutTemplateExercises.RemoveRange(template.Exercises);
        }
        else
        {
            template = new WorkoutTemplate
            {
                UserId = userId,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                IsActive = model.IsActive
            };

            _context.WorkoutTemplates.Add(template);
        }

        var displayUnit = await DisplayUnits.ForUserAsync(_context, userId);

        template.Exercises = model.Exercises
            .Select((exercise, index) => new WorkoutTemplateExercise
            {
                ExerciseId = exercise.ExerciseId,
                Order = index + 1,
                DefaultSets = exercise.DefaultSets,
                DefaultReps = exercise.DefaultReps,
                DefaultDurationSeconds = exercise.DefaultDurationSeconds,
                DefaultDistance = UnitConverter.ToCanonicalDistance(exercise.DefaultDistance, displayUnit),
                Notes = string.IsNullOrWhiteSpace(exercise.Notes) ? null : exercise.Notes.Trim()
            })
            .ToList();

        await _context.SaveChangesAsync();
        return template.Id;
    }

    public async Task<bool> DeleteTemplateAsync(int templateId, string userId)
    {
        var template = await _context.WorkoutTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId);

        if (template == null)
            return false;

        _context.WorkoutTemplates.Remove(template);
        await _context.SaveChangesAsync();
        return true;
    }
}

public class TemplateEditorModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Template name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set when the editor is showing a seeded template. The builder uses it to present a read-only
    /// preview with a copy action instead of an editable form; it is never trusted on the way back
    /// in, because <see cref="ITemplateService.SaveTemplateAsync"/> rejects built-ins by predicate.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    public List<TemplateExerciseEditorModel> Exercises { get; set; } = new();
}

public class TemplateExerciseEditorModel
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string MuscleGroups { get; set; } = string.Empty;

    // Every part of the prescription is optional (WDM-03); the builder still offers 3 x 10 as the
    // starting point for a newly added exercise, because that is what most of them want.
    [Range(1, 20)]
    public int? DefaultSets { get; set; }

    [Range(1, 50)]
    public int? DefaultReps { get; set; }

    [Range(1, 86400)]
    public int? DefaultDurationSeconds { get; set; }

    /// <summary>Prescribed distance in the user's display unit — converted on save, like any input.</summary>
    [Range(0.01, 1000)]
    public decimal? DefaultDistance { get; set; }

    public string? Notes { get; set; }
}
