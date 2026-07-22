using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Templates;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ITemplateService _templateService;
    private readonly IExerciseService _exerciseService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ITemplateService templateService, IExerciseService exerciseService, UserManager<ApplicationUser> userManager)
    {
        _templateService = templateService;
        _exerciseService = exerciseService;
        _userManager = userManager;
    }

    [BindProperty]
    public TemplateEditorModel Template { get; set; } = new();

    [BindProperty]
    public int? SelectedExerciseId { get; set; }

    public Dictionary<string, List<Exercise>> ExercisesByCategory { get; set; } = new();
    public bool IsEditMode => Template.Id.HasValue;

    /// <summary>Labels the distance input; the service converts it on the way in and out.</summary>
    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (id.HasValue)
        {
            var template = await _templateService.GetTemplateEditorAsync(id.Value, userId);
            if (template == null)
                return NotFound();

            // The read is deliberately wide enough to see built-ins, but this page only edits.
            // A hand-typed URL therefore lands on the list, where the offered action is Copy.
            if (template.IsBuiltIn)
                return RedirectToPage("/Templates/Index");

            Template = template;
        }

        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddExerciseAsync()
    {
        if (!await EnsureAuthenticatedAsync())
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (!SelectedExerciseId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Select an exercise to add.");
            await LoadPageDataAsync();
            return Page();
        }

        if (Template.Exercises.Any(e => e.ExerciseId == SelectedExerciseId.Value))
        {
            ModelState.AddModelError(string.Empty, "That exercise is already in the template.");
            await LoadPageDataAsync();
            return Page();
        }

        var exercise = await _exerciseService.GetExerciseAsync(SelectedExerciseId.Value);
        if (exercise == null)
            return NotFound();

        Template.Exercises.Add(new TemplateExerciseEditorModel
        {
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            Equipment = exercise.Equipment,
            MuscleGroups = exercise.MuscleGroups,
            // The prescription is optional, but a starting point beats an empty row for the
            // straightforward case; duration and distance stay blank until asked for.
            DefaultSets = 3,
            DefaultReps = 10
        });

        SelectedExerciseId = null;
        ModelState.Remove(nameof(SelectedExerciseId));
        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveExerciseAsync(int index)
    {
        if (!await EnsureAuthenticatedAsync())
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index >= 0 && index < Template.Exercises.Count)
            Template.Exercises.RemoveAt(index);

        ResetPostedRowValues();
        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMoveUpAsync(int index)
    {
        if (!await EnsureAuthenticatedAsync())
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index > 0 && index < Template.Exercises.Count)
            (Template.Exercises[index - 1], Template.Exercises[index]) = (Template.Exercises[index], Template.Exercises[index - 1]);

        ResetPostedRowValues();
        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMoveDownAsync(int index)
    {
        if (!await EnsureAuthenticatedAsync())
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index >= 0 && index < Template.Exercises.Count - 1)
            (Template.Exercises[index + 1], Template.Exercises[index]) = (Template.Exercises[index], Template.Exercises[index + 1]);

        ResetPostedRowValues();
        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        // The name rule is declared once, as [Required]/[StringLength] on TemplateEditorModel, so
        // model binding enforces it and the tag helper projects it into the markup.
        if (!Template.Exercises.Any())
            ModelState.AddModelError(string.Empty, "Add at least one exercise before saving.");

        // The per-exercise bounds are declared once, as [Range] on TemplateExerciseEditorModel, and
        // are enforced by model binding and projected into the markup by Html5ValidationTagHelper.
        // Restating them here would be a second copy of the same rule (WDM-UI-13).
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }

        var templateId = await _templateService.SaveTemplateAsync(userId, Template);
        if (!templateId.HasValue)
        {
            // Either an exercise id that no longer exists, or a template this user does not own —
            // the service does not distinguish, and neither case should leak which it was.
            ModelState.AddModelError(string.Empty, "Unable to save the template.");
            await LoadPageDataAsync();
            return Page();
        }

        return RedirectToPage("/Templates/Index");
    }

    /// <summary>
    /// Everything the view needs that is not round-tripped through the form. Every handler that
    /// returns Page() calls this, which is what stops a re-render path from quietly losing the
    /// exercise library or falling back to the wrong distance unit.
    /// </summary>
    private async Task LoadPageDataAsync()
    {
        ExercisesByCategory = (await _exerciseService.SearchExercisesAsync(null, null, null))
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);
    }

    /// <summary>
    /// Rows are re-rendered by list index, and the input tag helper reads ModelState in preference
    /// to the model — so after a move or a removal the posted values would stay against the old
    /// indices and the reorder would appear to do nothing. Binding has already copied every typed
    /// field onto the model, so clearing loses no user input.
    /// </summary>
    private void ResetPostedRowValues() => ModelState.Clear();

    private async Task<bool> EnsureAuthenticatedAsync()
    {
        return await _userManager.GetUserAsync(User) != null;
    }
}
