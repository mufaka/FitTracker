using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;

namespace FitTracker.Pages.Workouts;

[Authorize]
public class StartModel : PageModel
{
    public class ProgressiveOverloadSuggestion
    {
        public DateTime LastPerformedOn { get; set; }
        public int LastSetCount { get; set; }
        public int? LastTopReps { get; set; }
        public decimal? LastTopWeight { get; set; }
        public decimal LastTotalVolume { get; set; }
        public int? SuggestedReps { get; set; }
        public decimal? SuggestedWeight { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StartModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
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

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userManager.GetUserAsync(User);
        DefaultRestTimerSeconds = user?.DefaultRestTimer > 0 ? user.DefaultRestTimer : 90;
        UserUnits = string.IsNullOrWhiteSpace(user?.PreferredUnits) ? "lbs" : user!.PreferredUnits!;

        // Load all exercises grouped by category
        ExercisesByCategory = (await _context.Exercises
            .OrderBy(e => e.Name)
            .ToListAsync())
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Check for existing workout or create new one
        if (id.HasValue)
        {
            Workout = await _context.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Exercise)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .FirstOrDefaultAsync(w => w.Id == id.Value && w.UserId == userId);

            if (Workout == null)
                return NotFound();
        }
        else
        {
            // Check for today's unfinished workout
            var today = DateTime.UtcNow.Date;
            Workout = await _context.Workouts
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Exercise)
                .Include(w => w.WorkoutExercises)
                    .ThenInclude(we => we.Sets)
                .Where(w => w.UserId == userId &&
                            w.Date.Date == today &&
                            !w.IsCompleted)
                .OrderByDescending(w => w.Date)
                .FirstOrDefaultAsync();

            // Create new workout if none exists
            if (Workout == null)
            {
                Workout = new Workout
                {
                    UserId = userId,
                    Date = DateTime.UtcNow,
                    IsCompleted = false
                };
                _context.Workouts.Add(Workout);
                await _context.SaveChangesAsync();
            }
        }

        WorkoutId = Workout.Id;
        WorkoutExercises = Workout.WorkoutExercises.OrderBy(we => we.Order).ToList();
        await LoadProgressiveOverloadSuggestionsAsync(userId);

        return Page();
    }

    private async Task LoadProgressiveOverloadSuggestionsAsync(string userId)
    {
        ProgressiveOverloadSuggestions.Clear();

        if (!WorkoutExercises.Any())
            return;

        var exerciseIds = WorkoutExercises
            .Select(we => we.ExerciseId)
            .Distinct()
            .ToList();

        var completedExerciseHistory = await _context.WorkoutExercises
            .AsNoTracking()
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .Where(we => exerciseIds.Contains(we.ExerciseId) &&
                         we.Workout.UserId == userId &&
                         we.Workout.IsCompleted &&
                         we.Sets.Any())
            .OrderByDescending(we => we.Workout.Date)
            .ToListAsync();

        foreach (var exerciseId in exerciseIds)
        {
            var lastPerformance = completedExerciseHistory.FirstOrDefault(we => we.ExerciseId == exerciseId);
            if (lastPerformance == null)
                continue;

            var orderedSets = lastPerformance.Sets
                .OrderBy(s => s.SetNumber)
                .ToList();

            var weightedTopSet = orderedSets
                .Where(s => s.Weight.HasValue && s.Reps.HasValue)
                .OrderByDescending(s => s.Weight)
                .ThenByDescending(s => s.Reps)
                .FirstOrDefault();

            var bestRepSet = orderedSets
                .Where(s => s.Reps.HasValue)
                .OrderByDescending(s => s.Reps)
                .ThenByDescending(s => s.Weight ?? 0)
                .FirstOrDefault();

            var suggestion = new ProgressiveOverloadSuggestion
            {
                LastPerformedOn = lastPerformance.Workout.Date,
                LastSetCount = orderedSets.Count,
                LastTopWeight = weightedTopSet?.Weight,
                LastTopReps = weightedTopSet?.Reps ?? bestRepSet?.Reps,
                LastTotalVolume = orderedSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
            };

            if (weightedTopSet?.Weight.HasValue == true && weightedTopSet.Reps.HasValue)
            {
                var lastWeight = weightedTopSet.Weight.Value;
                var lastReps = weightedTopSet.Reps.Value;

                if (lastReps >= 8)
                {
                    suggestion.SuggestedWeight = lastWeight + GetSuggestedWeightIncrement(lastWeight);
                    suggestion.SuggestedReps = lastReps;
                    suggestion.Recommendation = $"Try {suggestion.SuggestedWeight:0.##} {UserUnits} for {suggestion.SuggestedReps} reps on your top set.";
                }
                else
                {
                    suggestion.SuggestedWeight = lastWeight;
                    suggestion.SuggestedReps = lastReps + 1;
                    suggestion.Recommendation = $"Keep {lastWeight:0.##} {UserUnits} on the bar and aim for {suggestion.SuggestedReps} reps.";
                }
            }
            else if (bestRepSet?.Reps.HasValue == true)
            {
                suggestion.SuggestedReps = bestRepSet.Reps.Value + 1;
                suggestion.Recommendation = $"Aim to beat your last best set with {suggestion.SuggestedReps} reps.";
            }
            else
            {
                suggestion.Recommendation = "Repeat the movement and focus on cleaner execution than last time.";
            }

            ProgressiveOverloadSuggestions[exerciseId] = suggestion;
        }
    }

    private static decimal GetSuggestedWeightIncrement(decimal weight)
    {
        return weight < 50 ? 2.5m : 5m;
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

        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(w => w.Id == WorkoutId && w.UserId == userId);

        if (workout == null || workout.IsCompleted)
            return await OnGetAsync(WorkoutId);

        var exercise = await _context.Exercises.FindAsync(SelectedExerciseId.Value);
        if (exercise == null)
            return await OnGetAsync(WorkoutId);

        var workoutExercise = new WorkoutExercise
        {
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            Order = workout.WorkoutExercises.Count + 1
        };

        _context.WorkoutExercises.Add(workoutExercise);
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostAddSetAsync(int workoutExerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var workoutExercise = await _context.WorkoutExercises
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .FirstOrDefaultAsync(we => we.Id == workoutExerciseId && 
                                      we.Workout.UserId == userId &&
                                      !we.Workout.IsCompleted);

        if (workoutExercise == null)
            return await OnGetAsync(WorkoutId);

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

        var set = new Set
        {
            WorkoutExerciseId = workoutExerciseId,
            SetNumber = workoutExercise.Sets.Count + 1,
            Weight = weight,
            Reps = reps,
            RPE = rpe
        };

        _context.Sets.Add(set);
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostRemoveSetAsync(int setId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var set = await _context.Sets
            .Include(s => s.WorkoutExercise)
                .ThenInclude(we => we.Workout)
            .FirstOrDefaultAsync(s => s.Id == setId && 
                                     s.WorkoutExercise.Workout.UserId == userId &&
                                     !s.WorkoutExercise.Workout.IsCompleted);

        if (set != null)
        {
            _context.Sets.Remove(set);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostRemoveExerciseAsync(int exerciseId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var workoutExercise = await _context.WorkoutExercises
            .Include(we => we.Workout)
            .Include(we => we.Sets)
            .FirstOrDefaultAsync(we => we.Id == exerciseId && 
                                      we.Workout.UserId == userId &&
                                      !we.Workout.IsCompleted);

        if (workoutExercise != null)
        {
            _context.WorkoutExercises.Remove(workoutExercise);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostCompleteWorkoutAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == WorkoutId && w.UserId == userId);

        if (workout == null)
            return NotFound();

        if (!workout.WorkoutExercises.Any())
        {
            ModelState.AddModelError("", "Add at least one exercise before completing the workout");
            return await OnGetAsync(WorkoutId);
        }

        // Calculate workout duration in minutes
        var duration = (int)(DateTime.UtcNow - workout.Date).TotalMinutes;

        workout.IsCompleted = true;
        workout.Duration = duration > 0 ? duration : 1;
        workout.Notes = WorkoutNotes;

        await _context.SaveChangesAsync();

        return RedirectToPage(new { id = WorkoutId });
    }

    public async Task<IActionResult> OnPostCancelWorkoutAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == WorkoutId && w.UserId == userId);

        if (workout != null)
        {
            _context.Workouts.Remove(workout);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("/Index");
    }
}
