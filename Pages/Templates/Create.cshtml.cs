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

            Template = template;
        }

        await LoadExerciseLibraryAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddExerciseAsync()
    {
        if (!await EnsureAuthenticatedAsync())
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (!SelectedExerciseId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Select an exercise to add.");
            await LoadExerciseLibraryAsync();
            return Page();
        }

        if (Template.Exercises.Any(e => e.ExerciseId == SelectedExerciseId.Value))
        {
            ModelState.AddModelError(string.Empty, "That exercise is already in the template.");
            await LoadExerciseLibraryAsync();
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
            MuscleGroups = exercise.MuscleGroups
        });

        SelectedExerciseId = null;
        await LoadExerciseLibraryAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveExerciseAsync(int index)
    {
        if (!await EnsureAuthenticatedAsync())
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index >= 0 && index < Template.Exercises.Count)
            Template.Exercises.RemoveAt(index);

        await LoadExerciseLibraryAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMoveUpAsync(int index)
    {
        if (!await EnsureAuthenticatedAsync())
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index > 0 && index < Template.Exercises.Count)
            (Template.Exercises[index - 1], Template.Exercises[index]) = (Template.Exercises[index], Template.Exercises[index - 1]);

        await LoadExerciseLibraryAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMoveDownAsync(int index)
    {
        if (!await EnsureAuthenticatedAsync())
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (index >= 0 && index < Template.Exercises.Count - 1)
            (Template.Exercises[index + 1], Template.Exercises[index]) = (Template.Exercises[index], Template.Exercises[index + 1]);

        await LoadExerciseLibraryAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (string.IsNullOrWhiteSpace(Template.Name))
            ModelState.AddModelError("Template.Name", "Template name is required.");

        if (!Template.Exercises.Any())
            ModelState.AddModelError(string.Empty, "Add at least one exercise before saving.");

        for (var i = 0; i < Template.Exercises.Count; i++)
        {
            if (Template.Exercises[i].DefaultSets < 1)
                ModelState.AddModelError($"Template.Exercises[{i}].DefaultSets", "Sets must be at least 1.");

            if (Template.Exercises[i].DefaultReps < 1)
                ModelState.AddModelError($"Template.Exercises[{i}].DefaultReps", "Reps must be at least 1.");
        }

        if (!ModelState.IsValid)
        {
            await LoadExerciseLibraryAsync();
            return Page();
        }

        try
        {
            var templateId = await _templateService.SaveTemplateAsync(userId, Template);
            if (!templateId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Unable to save the template.");
                await LoadExerciseLibraryAsync();
                return Page();
            }
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToPage("/Templates/Index");
    }

    private async Task LoadExerciseLibraryAsync()
    {
        ExercisesByCategory = (await _exerciseService.SearchExercisesAsync(null, null, null))
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<bool> EnsureAuthenticatedAsync()
    {
        return await _userManager.GetUserAsync(User) != null;
    }
}
