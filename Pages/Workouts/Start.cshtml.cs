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
    private readonly IWorkoutPlanService _workoutPlanService;
    private readonly UserManager<ApplicationUser> _userManager;

    public StartModel(
        IWorkoutService workoutService,
        IExerciseService exerciseService,
        IWorkoutPlanService workoutPlanService,
        UserManager<ApplicationUser> userManager)
    {
        _workoutService = workoutService;
        _exerciseService = exerciseService;
        _workoutPlanService = workoutPlanService;
        _userManager = userManager;
    }

    [BindProperty]
    public int? WorkoutId { get; set; }

    [BindProperty]
    public int? SelectedExerciseId { get; set; }

    [BindProperty]
    public string? WorkoutNotes { get; set; }

    /// <summary>
    /// Opt-out of the focused flow, back to the whole lineup (WDM-UI-17). This way round because a
    /// workout is performed on a phone, one exercise at a time, and that is the default; the list is
    /// for building and reviewing. Carried in the query string so it survives the redirect after every
    /// log — a preference this small does not earn a column, and reading it server-side keeps the flow
    /// from flashing the very list it exists to hide (Alpine is deferred).
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "list")]
    public bool ListMode { get; set; }

    /// <summary>
    /// Whether the screen is showing one exercise at a time. A completed workout is always the full
    /// list: there is nothing left to walk through, and reviewing wants everything at once.
    /// </summary>
    public bool FocusMode { get; private set; }

    /// <summary>
    /// The exercise the flow is on, by <c>WorkoutExercise.Id</c>. Absent means "wherever the flow has
    /// got to", which is how answering the last open exercise lands on the finish screen.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "at")]
    public int? FocusedId { get; set; }

    public int DefaultRestTimerSeconds { get; set; } = 90;
    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;
    public Workout? Workout { get; set; }
    public List<WorkoutExercise> WorkoutExercises { get; set; } = new();
    public Dictionary<string, List<Exercise>> ExercisesByCategory { get; set; } = new();
    public Dictionary<int, ProgressiveOverloadSuggestion> ProgressiveOverloadSuggestions { get; set; } = new();

    /// <summary>The plan guiding this workout, if any. Read live, never copied onto the workout.</summary>
    public WorkoutPlan? Plan { get; set; }

    /// <summary>
    /// The plan's prescription for each materialized exercise, keyed by <c>WorkoutExercise.Id</c>.
    /// Empty for an ad-hoc workout, and for any exercise the user added that the plan does not name.
    /// </summary>
    public Dictionary<int, WorkoutPlanExercise> PlanGuidance { get; set; } = new();

    /// <summary>
    /// The exercise focus mode is showing. Null once every exercise has been answered for — the flow
    /// has nothing left to put in front of the user, so the page offers to finish instead.
    /// </summary>
    public WorkoutExercise? FocusedExercise { get; set; }

    /// <summary>Its 1-based place in the workout, for "exercise 3 of 7".</summary>
    public int FocusedPosition { get; set; }

    /// <summary>How many exercises have been performed or skipped, for the progress readout.</summary>
    public int AddressedCount { get; set; }

    /// <summary>
    /// The exercises either side of the focused one, in workout order. The flow proposes an order but
    /// never imposes one (WDM-UI-08), so stepping is always available in both directions.
    /// </summary>
    public int? PreviousExerciseId { get; set; }

    public int? NextExerciseId { get; set; }

    /// <summary>
    /// Whether htmx made this request, and so wants the form back rather than a whole page or a
    /// redirect (WDM-UI-20). Its absence is the no-JavaScript path, which still works.
    /// </summary>
    private bool IsHtmxRequest => Request.Headers.ContainsKey("HX-Request");

    public async Task<IActionResult> OnGetAsync(int? id, int? planId = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (id.HasValue)
        {
            Workout = await _workoutService.GetWorkoutAsync(id.Value, userId);

            if (Workout == null)
                return NotFound();
        }
        else if (planId.HasValue)
        {
            Workout = await _workoutService.StartWorkoutFromPlanAsync(planId.Value, userId);

            // null covers not-yours, retired and deleted alike; the page says no more than that.
            if (Workout == null)
                return NotFound();
        }
        else
        {
            Workout = await _workoutService.StartWorkoutAsync(userId);
        }

        await LoadAsync(userId);

        // Stepping between exercises swaps the form; the rest of the page is untouched and its
        // Alpine state — a running rest timer above all — survives.
        return IsHtmxRequest ? Partial("_WorkoutForm", this) : Page();
    }

    /// <summary>
    /// Everything the view needs about a workout already resolved into <see cref="Workout"/>. Shared
    /// by the initial render and by every action that swaps the form back in, so the two can never
    /// disagree about what the screen is showing.
    /// </summary>
    private async Task LoadAsync(string userId)
    {
        var user = await _userManager.GetUserAsync(User);
        DefaultRestTimerSeconds = user?.DefaultRestTimer > 0 ? user.DefaultRestTimer : 90;
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        ExercisesByCategory = (await _exerciseService.SearchExercisesAsync(null, null, null))
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        WorkoutId = Workout!.Id;
        WorkoutExercises = Workout.WorkoutExercises.OrderBy(we => we.Order).ToList();

        // Only fall back to what is stored: on a post the notes came from the form, and overwriting
        // them here would throw away whatever the user had typed but not yet completed with.
        WorkoutNotes ??= Workout.Notes;

        ProgressiveOverloadSuggestions = await _workoutService.GetProgressiveOverloadSuggestionsAsync(
            userId,
            WorkoutExercises.Select(we => we.ExerciseId),
            UserUnits);

        await LoadPlanGuidanceAsync(userId);
        ResolveFocus();
    }

    /// <summary>
    /// The result of an action: the form on its own for htmx, and otherwise the redirect that has
    /// always followed a post here. <paramref name="focusedId"/> means the same in both — null is
    /// "wherever the flow has got to", which is what carries the user to the finish screen.
    /// </summary>
    private async Task<IActionResult> RespondAsync(string userId, int? focusedId)
    {
        if (!IsHtmxRequest)
            return RedirectToWorkout(focusedId);

        FocusedId = focusedId;

        Workout = WorkoutId.HasValue ? await _workoutService.GetWorkoutAsync(WorkoutId.Value, userId) : null;
        if (Workout == null)
            return NotFound();

        await LoadAsync(userId);

        // Keep the address bar honest. Without this a reload after several actions would replay the
        // URL the workout was opened with — and `?planId=` opens a workout rather than continuing one.
        Response.Headers["HX-Push-Url"] = ListMode
            ? Url.Page("/Workouts/Start", new { id = WorkoutId, list = true })!
            : Url.Page("/Workouts/Start", new { id = WorkoutId, at = FocusedId })!;

        return Partial("_WorkoutForm", this);
    }

    /// <summary>
    /// Works out which single exercise the flow is showing and where it sits, from the id the request
    /// carried. Cheap enough to run either way, but only the focused view reads the result.
    /// </summary>
    private void ResolveFocus()
    {
        AddressedCount = GuidedWorkoutFlow.AddressedCount(WorkoutExercises);
        FocusMode = !ListMode && WorkoutExercises.Count > 0 && Workout?.IsCompleted == false;

        if (!FocusMode)
            return;

        FocusedExercise = GuidedWorkoutFlow.Resolve(WorkoutExercises, FocusedId);
        FocusedId = FocusedExercise?.Id;

        if (FocusedExercise == null)
            return;

        var index = WorkoutExercises.IndexOf(FocusedExercise);
        FocusedPosition = index + 1;
        PreviousExerciseId = index > 0 ? WorkoutExercises[index - 1].Id : null;
        NextExerciseId = index < WorkoutExercises.Count - 1 ? WorkoutExercises[index + 1].Id : null;
    }

    /// <summary>
    /// Back to the workout, keeping the flow where the caller says it should be. A null
    /// <paramref name="focusedId"/> means "wherever the flow has got to" rather than "stay put", which
    /// is what carries the user to the finish screen once nothing is left open.
    /// </summary>
    private IActionResult RedirectToWorkout(int? focusedId) =>
        ListMode
            ? RedirectToPage(new { id = WorkoutId, list = true })
            : RedirectToPage(new { id = WorkoutId, at = focusedId });

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

        // The addition goes on the end of the lineup; the flow stays on whatever the user was doing
        // and reaches it in turn.
        return await RespondAsync(userId, FocusedId);
    }

    public async Task<IActionResult> OnPostLogSetsAsync(int workoutExerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        // The set inputs are rows inside a single page-wide form covering every exercise, so they are
        // keyed by exercise id and set number rather than bound as a model. The span on offer comes
        // back with them; anything outside it belongs to a different exercise's rows.
        var first = ReadInt($"SetFirst_{workoutExerciseId}") ?? 1;
        var rows = ReadInt($"SetRows_{workoutExerciseId}") ?? 0;

        for (var setNumber = first; setNumber < first + rows; setNumber++)
        {
            var weight = ReadDecimal($"Weight_{workoutExerciseId}_{setNumber}");
            var reps = ReadInt($"Reps_{workoutExerciseId}_{setNumber}");
            var rpe = ReadInt($"RPE_{workoutExerciseId}_{setNumber}");
            var durationSeconds = ReadInt($"Duration_{workoutExerciseId}_{setNumber}");
            var distance = ReadDecimal($"Distance_{workoutExerciseId}_{setNumber}");

            // A prescribed row the user emptied is a set they did not do. The plan proposes; only
            // what was typed is recorded.
            if (weight is null && reps is null && rpe is null && durationSeconds is null && distance is null)
                continue;

            // Sequential rather than batched: each call numbers the set from what is already there,
            // so the rows land as 1, 2, 3 in the order they were filled in.
            await _workoutService.LogSetAsync(workoutExerciseId, userId, weight, reps, rpe, durationSeconds, distance);
        }

        // Logging is not finishing the exercise — the user still says how it went — so the flow stays
        // on it.
        return await RespondAsync(userId, workoutExerciseId);
    }

    public async Task<IActionResult> OnPostSetStatusAsync(int workoutExerciseId, string status)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        // A false result means the status is unknown, the exercise is not the caller's, or Skipped
        // was asked for on an exercise that has sets. None of those needs its own message: the page
        // re-renders showing the status that actually applies.
        var applied = await _workoutService.SetExerciseStatusAsync(workoutExerciseId, userId, status);

        // Clearing an answer is not answering, and a refused change moved nothing, so both stay put.
        if (ListMode || !applied || status == WorkoutExerciseStatuses.Pending)
            return await RespondAsync(userId, workoutExerciseId);

        var workout = WorkoutId.HasValue ? await _workoutService.GetWorkoutAsync(WorkoutId.Value, userId) : null;
        var ordered = workout?.WorkoutExercises.OrderBy(we => we.Order).ToList() ?? new List<WorkoutExercise>();

        // Null carries the user to the finish screen, which is exactly right once the answer just
        // given was the last one outstanding.
        return await RespondAsync(userId, GuidedWorkoutFlow.NextUnaddressed(ordered, workoutExerciseId)?.Id);
    }

    private decimal? ReadDecimal(string key) =>
        Request.Form.TryGetValue(key, out var value) && decimal.TryParse(value, out var parsed) ? parsed : null;

    private int? ReadInt(string key) =>
        Request.Form.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : null;

    /// <summary>
    /// Pairs each materialized exercise with the plan line it came from. Walking both lists in order
    /// and consuming each match once keeps a plan's deliberate duplicates — a warm-up's push-ups and
    /// the main block's — pointing at their own prescriptions rather than both at the first.
    /// </summary>
    private async Task LoadPlanGuidanceAsync(string userId)
    {
        if (Workout?.WorkoutPlanId is not int planId)
            return;

        Plan = await _workoutPlanService.GetPlanForGuidanceAsync(planId, userId);
        if (Plan == null)
            return;

        var unmatched = Plan.Exercises.OrderBy(pe => pe.Order).ToList();

        foreach (var workoutExercise in WorkoutExercises)
        {
            var match = unmatched.FirstOrDefault(pe => pe.ExerciseId == workoutExercise.ExerciseId);
            if (match == null)
                continue;

            PlanGuidance[workoutExercise.Id] = match;
            unmatched.Remove(match);
        }
    }

    public async Task<IActionResult> OnPostRemoveSetAsync(int setId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await _workoutService.RemoveSetAsync(setId, userId);

        return await RespondAsync(userId, FocusedId);
    }

    public async Task<IActionResult> OnPostRemoveExerciseAsync(int exerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await _workoutService.RemoveExerciseAsync(exerciseId, userId);

        // The removed exercise may be the one the flow was on, so let it settle wherever it lands.
        return await RespondAsync(userId, null);
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
