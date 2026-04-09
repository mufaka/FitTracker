using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;

namespace FitTracker.Pages.Exercises;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Exercise? Exercise { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastPerformed { get; set; }
    public Set? BestSet { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Exercise = await _context.Exercises.FindAsync(id);

        if (Exercise == null)
        {
            return NotFound();
        }

        // Get user-specific statistics if authenticated
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var workoutExercises = await _context.WorkoutExercises
                    .Include(we => we.Workout)
                    .Include(we => we.Sets)
                    .Where(we => we.ExerciseId == id && we.Workout.UserId == userId)
                    .ToListAsync();

                UsageCount = workoutExercises.Count;

                if (workoutExercises.Any())
                {
                    LastPerformed = workoutExercises
                        .Max(we => we.Workout.Date);

                    // Find best set (highest weight, or most reps if no weight)
                    var allSets = workoutExercises.SelectMany(we => we.Sets).ToList();
                    if (allSets.Any())
                    {
                        BestSet = allSets
                            .OrderByDescending(s => s.Weight ?? 0)
                            .ThenByDescending(s => s.Reps ?? 0)
                            .FirstOrDefault();
                    }
                }
            }
        }

        return Page();
    }
}
