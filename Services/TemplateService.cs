using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface ITemplateService
{
    Task<List<WorkoutTemplate>> GetTemplatesAsync(string userId);
    Task<List<WorkoutTemplate>> GetActiveTemplatesAsync(string userId, int count = 3);
    Task<TemplateEditorModel?> GetTemplateEditorAsync(int templateId, string userId);
    Task<int?> SaveTemplateAsync(string userId, TemplateEditorModel model);
    Task<bool> DeleteTemplateAsync(int templateId, string userId);
}

public class TemplateService : ITemplateService
{
    private readonly ApplicationDbContext _context;

    public TemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkoutTemplate>> GetTemplatesAsync(string userId)
    {
        return await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises)
                .ThenInclude(te => te.Exercise)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<WorkoutTemplate>> GetActiveTemplatesAsync(string userId, int count = 3)
    {
        return await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises)
                .ThenInclude(te => te.Exercise)
            .Where(t => t.UserId == userId && t.IsActive)
            .OrderBy(t => t.Name)
            .Take(count)
            .ToListAsync();
    }

    public async Task<TemplateEditorModel?> GetTemplateEditorAsync(int templateId, string userId)
    {
        var template = await _context.WorkoutTemplates
            .AsNoTracking()
            .Include(t => t.Exercises.OrderBy(te => te.Order))
                .ThenInclude(te => te.Exercise)
            .FirstOrDefaultAsync(t => t.Id == templateId && t.UserId == userId);

        if (template == null)
            return null;

        return new TemplateEditorModel
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive,
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
                    Notes = te.Notes
                })
                .ToList()
        };
    }

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
            template = await _context.WorkoutTemplates
                .Include(t => t.Exercises)
                .FirstOrDefaultAsync(t => t.Id == model.Id.Value && t.UserId == userId)
                ?? throw new InvalidOperationException("Template not found.");

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

        template.Exercises = model.Exercises
            .Select((exercise, index) => new WorkoutTemplateExercise
            {
                ExerciseId = exercise.ExerciseId,
                Order = index + 1,
                DefaultSets = exercise.DefaultSets,
                DefaultReps = exercise.DefaultReps,
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
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TemplateExerciseEditorModel> Exercises { get; set; } = new();
}

public class TemplateExerciseEditorModel
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string MuscleGroups { get; set; } = string.Empty;
    public int DefaultSets { get; set; } = 3;
    public int DefaultReps { get; set; } = 10;
    public string? Notes { get; set; }
}
