using System.ComponentModel.DataAnnotations;
using FitTracker.Data;
using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

public interface IWorkoutPlanService
{
    Task<List<WorkoutPlan>> GetPlansAsync(string userId);
    Task<List<WorkoutPlan>> GetActivePlansAsync(string userId, int count = 3);
    Task<PlanEditorModel?> GetPlanEditorAsync(int planId, string userId);
    Task<int?> SavePlanAsync(string userId, PlanEditorModel model);
    Task<PlanEditorModel?> ApplyTemplateAsync(int templateId, string userId, PlanEditorModel model);
    Task<bool> SetPlanActiveAsync(int planId, string userId, bool isActive);
    Task<bool> DeletePlanAsync(int planId, string userId);
    Task<bool> IsPlanReferencedAsync(int planId, string userId);
    Task<WorkoutPlan?> GetPlanForGuidanceAsync(int planId, string userId);
}

/// <summary>
/// Plans are always user-owned, so — unlike templates — every read and write here carries exactly
/// one predicate: <c>UserId == userId</c>, filtered inside the query. A method that forgets it is a
/// data leak rather than a compile error (WDM-SEC-01).
/// </summary>
public class WorkoutPlanService : IWorkoutPlanService
{
    private readonly ApplicationDbContext _context;
    private readonly ITemplateService _templateService;

    public WorkoutPlanService(ApplicationDbContext context, ITemplateService templateService)
    {
        _context = context;
        _templateService = templateService;
    }

    /// <summary>Convenience for tests, matching <see cref="WorkoutService"/>'s.</summary>
    public WorkoutPlanService(ApplicationDbContext context)
        : this(context, new TemplateService(context))
    {
    }

    public async Task<List<WorkoutPlan>> GetPlansAsync(string userId)
    {
        // Inactive plans are included: a retired plan has to stay visible to be reactivated (WDM-16).
        // Soft-deleted ones never are (WDM-17).
        return await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Exercises.OrderBy(pe => pe.Order))
                .ThenInclude(pe => pe.Exercise)
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<WorkoutPlan>> GetActivePlansAsync(string userId, int count = 3)
    {
        return await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Exercises.OrderBy(pe => pe.Order))
                .ThenInclude(pe => pe.Exercise)
            .Where(p => p.UserId == userId && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Take(count)
            .ToListAsync();
    }

    public async Task<PlanEditorModel?> GetPlanEditorAsync(int planId, string userId)
    {
        var plan = await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Exercises.OrderBy(pe => pe.Order))
                .ThenInclude(pe => pe.Exercise)
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId && !p.IsDeleted);

        if (plan == null)
            return null;

        var displayUnit = await DisplayUnits.ForUserAsync(_context, userId);

        return new PlanEditorModel
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            IsActive = plan.IsActive,
            Exercises = plan.Exercises
                .OrderBy(pe => pe.Order)
                .Select(pe => ToEditorExercise(pe, displayUnit))
                .ToList()
        };
    }

    public async Task<int?> SavePlanAsync(string userId, PlanEditorModel model)
    {
        var exerciseIds = model.Exercises.Select(e => e.ExerciseId).Distinct().ToList();
        var knownExerciseIds = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (knownExerciseIds.Count != exerciseIds.Count)
            return null;

        WorkoutPlan plan;
        if (model.Id.HasValue)
        {
            var existing = await _context.WorkoutPlans
                .Include(p => p.Exercises)
                .FirstOrDefaultAsync(p => p.Id == model.Id.Value && p.UserId == userId && !p.IsDeleted);

            if (existing == null)
                return null;

            plan = existing;
            plan.Name = model.Name.Trim();
            plan.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            plan.IsActive = model.IsActive;

            // Replace the lines wholesale, as the template editor does. Order is the list position,
            // so stable child ids would buy nothing.
            _context.WorkoutPlanExercises.RemoveRange(plan.Exercises);
        }
        else
        {
            plan = new WorkoutPlan
            {
                UserId = userId,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                IsActive = model.IsActive
            };

            _context.WorkoutPlans.Add(plan);
        }

        var displayUnit = await DisplayUnits.ForUserAsync(_context, userId);

        plan.Exercises = model.Exercises
            .Select((exercise, index) => new WorkoutPlanExercise
            {
                ExerciseId = exercise.ExerciseId,
                Order = index + 1,
                TargetSets = exercise.TargetSets,
                TargetReps = exercise.TargetReps,
                TargetDurationSeconds = exercise.TargetDurationSeconds,
                TargetDistance = UnitConverter.ToCanonicalDistance(exercise.TargetDistance, displayUnit),
                Notes = string.IsNullOrWhiteSpace(exercise.Notes) ? null : exercise.Notes.Trim()
            })
            .ToList();

        await _context.SaveChangesAsync();
        return plan.Id;
    }

    public async Task<PlanEditorModel?> ApplyTemplateAsync(int templateId, string userId, PlanEditorModel model)
    {
        // Asked for through the template service rather than re-derived here: what counts as a
        // "visible" template — the caller's own plus every built-in — is a rule that must live in
        // one place, or a future change to it will miss this site (WDM-SEC-03). This also hands
        // back distances already in the user's display unit, which is what the editor holds.
        var template = await _templateService.GetTemplateEditorAsync(templateId, userId);

        if (template == null)
            return null;

        // Append everything the template contributes, in its order, prescription and all — with no
        // merging, deduplication or dropping (WDM-12, WDM-13). An exercise the plan already holds is
        // kept as a second entry; the builder flags it and the user decides.
        foreach (var templateExercise in template.Exercises)
        {
            model.Exercises.Add(new PlanExerciseEditorModel
            {
                ExerciseId = templateExercise.ExerciseId,
                ExerciseName = templateExercise.ExerciseName,
                Equipment = templateExercise.Equipment,
                MuscleGroups = templateExercise.MuscleGroups,
                TargetSets = templateExercise.DefaultSets,
                TargetReps = templateExercise.DefaultReps,
                TargetDurationSeconds = templateExercise.DefaultDurationSeconds,
                TargetDistance = templateExercise.DefaultDistance,
                Notes = templateExercise.Notes
            });
        }

        // No record is kept of which templates a plan was built from (WDM-14).
        return model;
    }

    public async Task<bool> SetPlanActiveAsync(int planId, string userId, bool isActive)
    {
        var plan = await _context.WorkoutPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId && !p.IsDeleted);

        if (plan == null)
            return false;

        plan.IsActive = isActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePlanAsync(int planId, string userId)
    {
        var plan = await _context.WorkoutPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId && !p.IsDeleted);

        if (plan == null)
            return false;

        // Soft delete only. A workout may point at this plan, and that reference has to keep
        // resolving so the workout can still show what it was aiming at (WDM-17).
        plan.IsDeleted = true;
        plan.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsPlanReferencedAsync(int planId, string userId)
    {
        // Scoped to the caller's own workouts: whether somebody else has used a plan is not
        // something this user should be able to learn.
        return await _context.Workouts
            .AnyAsync(w => w.WorkoutPlanId == planId && w.UserId == userId);
    }

    public async Task<WorkoutPlan?> GetPlanForGuidanceAsync(int planId, string userId)
    {
        // Read live, never snapshotted (WDM-26), and deliberately unfiltered by IsActive/IsDeleted:
        // a workout performed months ago still has to be able to show what it was aiming at, even
        // if the plan has since been retired or deleted. Ownership is still enforced.
        return await _context.WorkoutPlans
            .AsNoTracking()
            .Include(p => p.Exercises.OrderBy(pe => pe.Order))
                .ThenInclude(pe => pe.Exercise)
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId);
    }

    private static PlanExerciseEditorModel ToEditorExercise(WorkoutPlanExercise planExercise, string displayUnit) => new()
    {
        ExerciseId = planExercise.ExerciseId,
        ExerciseName = planExercise.Exercise.Name,
        Equipment = planExercise.Exercise.Equipment,
        MuscleGroups = planExercise.Exercise.MuscleGroups,
        TargetSets = planExercise.TargetSets,
        TargetReps = planExercise.TargetReps,
        TargetDurationSeconds = planExercise.TargetDurationSeconds,
        TargetDistance = UnitConverter.ToDisplayDistance(planExercise.TargetDistance, displayUnit),
        Notes = planExercise.Notes
    };
}

public class PlanEditorModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Plan name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public List<PlanExerciseEditorModel> Exercises { get; set; } = new();

    /// <summary>
    /// Set by the builder once the user has acknowledged that workouts already reference this plan
    /// (WDM-18). It travels through the form so the warning is shown once rather than on every save.
    /// </summary>
    public bool ConfirmedEditOfUsedPlan { get; set; }
}

public class PlanExerciseEditorModel
{
    public int ExerciseId { get; set; }

    // Round-tripped through hidden fields for display only; the ids are what is trusted on save.
    public string ExerciseName { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string MuscleGroups { get; set; } = string.Empty;

    [Range(1, 20)]
    public int? TargetSets { get; set; }

    [Range(1, 50)]
    public int? TargetReps { get; set; }

    [Range(1, 86400)]
    public int? TargetDurationSeconds { get; set; }

    /// <summary>Prescribed distance in the user's display unit — converted on save.</summary>
    [Range(0.01, 1000)]
    public decimal? TargetDistance { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}
