using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FitTracker.Models;
using FitTracker.Services;

namespace FitTracker.Pages.Workouts;

[Authorize]
public class StartModel : PageModel
{
    private readonly IWorkoutService _workoutService;
    private readonly IExerciseService _exerciseService;
    private readonly UserManager<ApplicationUser> _userManager;

    public StartModel(IWorkoutService workoutService, IExerciseService exerciseService, UserManager<ApplicationUser> userManager)
    {
        _workoutService = workoutService;
        _exerciseService = exerciseService;
        _userManager = userManager;
    }

    [BindProperty]
    public int? WorkoutId { get; set; }

    [BindProperty]
    public int? SelectedExerciseId { get; set; }

    [BindProperty]
    public string? WorkoutNotes { get; set; }

    public int DefaultRestTimerSeconds { get; set; } = 90;
    public string UserUnits { get; set; } = "lbs";
    public Workout? Workout { get; set; }
    public List<WorkoutExercise> WorkoutExercises { get; set; } = new();
    public Dictionary<string, List<Exercise>> ExercisesByCategory { get; set; } = new();
    public Dictionary<int, ProgressiveOverloadSuggestion> ProgressiveOverloadSuggestions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id, int? templateId = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userManager.GetUserAsync(User);
        DefaultRestTimerSeconds = user?.DefaultRestTimer > 0 ? user.DefaultRestTimer : 90;
        UserUnits = string.IsNullOrWhiteSpace(user?.PreferredUnits) ? "lbs" : user!.PreferredUnits!;

        ExercisesByCategory = (await _exerciseService.SearchExercisesAsync(null, null, null))
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (id.HasValue)
        {
            Workout = await _workoutService.GetWorkoutAsync(id.Value, userId);

            if (Workout == null)
                return NotFound();
        }
        else if (templateId.HasValue)
        {
            Workout = await _workoutService.StartWorkoutFromTemplateAsync(templateId.Value, userId);

            if (Workout == null)
                return NotFound();
        }
        else
        {
            Workout = await _workoutService.StartWorkoutAsync(userId);
        }

        WorkoutId = Workout.Id;
        WorkoutExercises = Workout.WorkoutExercises.OrderBy(we => we.Order).ToList();
        WorkoutNotes = Workout.Notes;
        ProgressiveOverloadSuggestions = await _workoutService.GetProgressiveOverloadSuggestionsAsync(
            userId,
            WorkoutExercises.Select(we => we.ExerciseId),
            UserUnits);

        return Page();
    }

    public async Task<IActionResult> OnPostAddExerciseAsync()
    {
        if (!SelectedExerciseId.HasValue)
        {
            ModelState.AddModelError("", "Please select an exercise");
            return await OnGetAsync(WorkoutId);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (!WorkoutId.HasValue)
            return await OnGetAsync(null);

        await _workoutService.AddExerciseToWorkoutAsync(WorkoutId.Value, SelectedExerciseId.Value, userId);

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostAddSetAsync(int workoutExerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        // Get form values
        var weightKey = $"Weight_{workoutExerciseId}";
        var repsKey = $"Reps_{workoutExerciseId}";
        var rpeKey = $"RPE_{workoutExerciseId}";

        decimal? weight = null;
        int? reps = null;
        int? rpe = null;

        if (Request.Form.ContainsKey(weightKey) && decimal.TryParse(Request.Form[weightKey], out var w))
            weight = w;

        if (Request.Form.ContainsKey(repsKey) && int.TryParse(Request.Form[repsKey], out var r))
            reps = r;

        if (Request.Form.ContainsKey(rpeKey) && int.TryParse(Request.Form[rpeKey], out var rp))
            rpe = rp;

        await _workoutService.LogSetAsync(workoutExerciseId, userId, weight, reps, rpe);

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostRemoveSetAsync(int setId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await _workoutService.RemoveSetAsync(setId, userId);

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostRemoveExerciseAsync(int exerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await _workoutService.RemoveExerciseAsync(exerciseId, userId);

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostCompleteWorkoutAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (!WorkoutId.HasValue)
            return NotFound();

        var result = await _workoutService.CompleteWorkoutAsync(WorkoutId.Value, userId, WorkoutNotes);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.ErrorMessage ?? "Unable to complete workout.");
            return await OnGetAsync(WorkoutId);
        }

        if (result.UnlockedAchievementIds.Any())
        {
            TempData["UnlockedAchievementIds"] = string.Join(',', result.UnlockedAchievementIds);
        }

        return RedirectToPage("/Workouts/Details", new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostCancelWorkoutAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (WorkoutId.HasValue)
        {
            await _workoutService.CancelWorkoutAsync(WorkoutId.Value, userId);
        }

        return RedirectToPage("/Index");
    }
}
