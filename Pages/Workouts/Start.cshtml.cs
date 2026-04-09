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

    public Workout? Workout { get; set; }
    public List<WorkoutExercise> WorkoutExercises { get; set; } = new();
    public Dictionary<string, List<Exercise>> ExercisesByCategory { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

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
                .FirstOrDefaultAsync(w => w.UserId == userId && 
                                        w.Date.Date == today && 
                                        !w.IsCompleted);

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
