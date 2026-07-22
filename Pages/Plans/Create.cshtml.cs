using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Plans;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IWorkoutPlanService _planService;
    private readonly ITemplateService _templateService;
    private readonly IExerciseService _exerciseService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(
        IWorkoutPlanService planService,
        ITemplateService templateService,
        IExerciseService exerciseService,
        UserManager<ApplicationUser> userManager)
    {
        _planService = planService;
        _templateService = templateService;
        _exerciseService = exerciseService;
        _userManager = userManager;
    }

    [BindProperty]
    public PlanEditorModel Plan { get; set; } = new();

    [BindProperty]
    public int? SelectedExerciseId { get; set; }

    [BindProperty]
    public int? SelectedTemplateId { get; set; }

    public Dictionary<string, List<Exercise>> ExercisesByCategory { get; set; } = new();

    /// <summary>Templates the user owns, offered separately from the seeded catalog (WDM-UI-01).</summary>
    public List<WorkoutTemplate> PersonalTemplates { get; set; } = new();
    public List<WorkoutTemplate> BuiltInTemplates { get; set; } = new();

    /// <summary>
    /// Exercise ids that occupy more than one row. Duplicates are legitimate — a warm-up's light
    /// push-ups are not the main block's — so this only drives a badge (WDM-UI-02, WDM-13).
    /// </summary>
    public HashSet<int> DuplicateExerciseIds { get; set; } = new();

    /// <summary>Set when a save was held back until the user acknowledges WDM-18.</summary>
    public bool RequiresEditConfirmation { get; set; }

    public bool IsEditMode => Plan.Id.HasValue;

    /// <summary>Labels the distance input; the service converts it on the way in and out.</summary>
    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (id.HasValue)
        {
            var plan = await _planService.GetPlanEditorAsync(id.Value, userId);
            if (plan == null)
                return NotFound();

            Plan = plan;
        }

        await LoadPageDataAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostApplyTemplateAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (!SelectedTemplateId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Select a template to apply.");
            await LoadPageDataAsync(userId);
            return Page();
        }

        // Appends; it never merges or drops what the plan already holds (WDM-12, WDM-13). Existing
        // rows keep their index, so the posted values stay aligned with the model.
        var applied = await _planService.ApplyTemplateAsync(SelectedTemplateId.Value, userId, Plan);
        if (applied == null)
            return NotFound();

        Plan = applied;

        // Clear the picker so the next apply is a deliberate choice. The posted value has to go
        // with it, or the re-rendered select reads ModelState and stays on the applied template.
        SelectedTemplateId = null;
        ModelState.Remove(nameof(SelectedTemplateId));

        await LoadPageDataAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAddExerciseAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (!SelectedExerciseId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Select an exercise to add.");
            await LoadPageDataAsync(userId);
            return Page();
        }

        var exercise = await _exerciseService.GetExerciseAsync(SelectedExerciseId.Value);
        if (exercise == null)
            return NotFound();

        // A plan is a flat ordered list, so an exercise already present is added again rather than
        // refused — the duplicate badge tells the user, and the user decides (WDM-13).
        Plan.Exercises.Add(new PlanExerciseEditorModel
        {
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            Equipment = exercise.Equipment,
            MuscleGroups = exercise.MuscleGroups,
            // Every target is optional, but a starting point beats an empty row for the
            // straightforward case; duration and distance stay blank until asked for.
            TargetSets = 3,
            TargetReps = 10
        });

        SelectedExerciseId = null;
        ModelState.Remove(nameof(SelectedExerciseId));

        await LoadPageDataAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveExerciseAsync(int index)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index >= 0 && index < Plan.Exercises.Count)
            Plan.Exercises.RemoveAt(index);

        ResetPostedRowValues();
        await LoadPageDataAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostMoveUpAsync(int index)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index > 0 && index < Plan.Exercises.Count)
            (Plan.Exercises[index - 1], Plan.Exercises[index]) = (Plan.Exercises[index], Plan.Exercises[index - 1]);

        ResetPostedRowValues();
        await LoadPageDataAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostMoveDownAsync(int index)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index >= 0 && index < Plan.Exercises.Count - 1)
            (Plan.Exercises[index + 1], Plan.Exercises[index]) = (Plan.Exercises[index], Plan.Exercises[index + 1]);

        ResetPostedRowValues();
        await LoadPageDataAsync(userId);
        return Page();
    }

    public Task<IActionResult> OnPostSaveAsync() => SavePlanAsync();

    /// <summary>
    /// The button that acknowledges the WDM-18 warning. It is the click itself that confirms, so the
    /// posted flag is still false and its stale entry is dropped along with it — a re-rendered hidden
    /// input reads ModelState in preference to the model.
    /// </summary>
    public Task<IActionResult> OnPostConfirmSaveAsync()
    {
        Plan.ConfirmedEditOfUsedPlan = true;
        ModelState.Remove($"{nameof(Plan)}.{nameof(Plan.ConfirmedEditOfUsedPlan)}");

        return SavePlanAsync();
    }

    private async Task<IActionResult> SavePlanAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (!Plan.Exercises.Any())
            ModelState.AddModelError(string.Empty, "Add at least one exercise before saving.");

        // Name length and the per-exercise bounds are declared once, as DataAnnotations on
        // PlanEditorModel, enforced by model binding and projected into the markup by
        // Html5ValidationTagHelper. Restating them here would be a second copy (WDM-UI-13).
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync(userId);
            return Page();
        }

        // A plan is read live during a workout, so editing one that has already been trained from
        // changes what those workouts show as their intent. No version is kept, which is why the
        // user is told before the edit lands rather than after (WDM-18, WDM-UI-04).
        if (Plan.Id.HasValue &&
            !Plan.ConfirmedEditOfUsedPlan &&
            await _planService.IsPlanReferencedAsync(Plan.Id.Value, userId))
        {
            RequiresEditConfirmation = true;
            await LoadPageDataAsync(userId);
            return Page();
        }

        var planId = await _planService.SavePlanAsync(userId, Plan);
        if (!planId.HasValue)
        {
            // Either an exercise id that no longer exists, or a plan this user does not own — the
            // service does not distinguish, and neither case should leak which it was.
            ModelState.AddModelError(string.Empty, "Unable to save the plan.");
            await LoadPageDataAsync(userId);
            return Page();
        }

        return RedirectToPage("/Plans/Index");
    }

    /// <summary>
    /// Everything the view needs that is not round-tripped through the form. Every handler that
    /// returns Page() calls this, which is what stops a re-render path from quietly losing the
    /// exercise library, the template picker or the duplicate badges.
    /// </summary>
    private async Task LoadPageDataAsync(string userId)
    {
        ExercisesByCategory = (await _exerciseService.SearchExercisesAsync(null, null, null))
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        // The read already spans personal and built-in templates; the picker only splits the result.
        var templates = await _templateService.GetTemplatesAsync(userId);
        PersonalTemplates = templates.Where(t => !t.IsBuiltIn).ToList();
        BuiltInTemplates = templates.Where(t => t.IsBuiltIn).ToList();

        DuplicateExerciseIds = Plan.Exercises
            .GroupBy(e => e.ExerciseId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);
    }

    /// <summary>
    /// Discards the posted values after rows have changed position. The input tag helper renders
    /// from ModelState in preference to the model, so without this a move or a removal would
    /// re-render the old order against the new indices. Nothing is lost: model binding has already
    /// copied every field the user typed onto <see cref="Plan"/>.
    /// </summary>
    private void ResetPostedRowValues() => ModelState.Clear();
}
