using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;

namespace FitTracker.Pages.Workouts;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Workout? Workout { get; set; }
    public int TotalSets { get; set; }
    public int TotalReps { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageRPE { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        Workout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (Workout == null)
            return NotFound();

        // Calculate statistics
        var allSets = Workout.WorkoutExercises.SelectMany(we => we.Sets).ToList();
        TotalSets = allSets.Count;
        TotalReps = allSets.Sum(s => s.Reps ?? 0);
        TotalVolume = allSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
        
        var setsWithRPE = allSets.Where(s => s.RPE.HasValue).ToList();
        AverageRPE = setsWithRPE.Any() ? (decimal)setsWithRPE.Average(s => s.RPE!.Value) : 0;

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var workout = await _context.Workouts
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workout != null)
        {
            _context.Workouts.Remove(workout);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("/Workouts/History");
    }

    public async Task<IActionResult> OnPostRepeatAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var sourceWorkout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (sourceWorkout == null)
            return NotFound();

        var repeatedWorkout = new Workout
        {
            UserId = userId,
            Date = DateTime.UtcNow,
            IsCompleted = false
        };

        foreach (var exercise in sourceWorkout.WorkoutExercises.OrderBy(we => we.Order))
        {
            repeatedWorkout.WorkoutExercises.Add(new WorkoutExercise
            {
                ExerciseId = exercise.ExerciseId,
                Order = exercise.Order,
                Notes = exercise.Notes
            });
        }

        _context.Workouts.Add(repeatedWorkout);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Workouts/Start", new { id = repeatedWorkout.Id });
    }
}
