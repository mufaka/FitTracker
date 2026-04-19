using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FitTracker.Models;
using FitTracker.Services;

namespace FitTracker.Pages.Exercises;

public class DetailsModel : PageModel
{
    private readonly IExerciseService _exerciseService;
    private readonly IPersonalRecordService _personalRecordService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(IExerciseService exerciseService, IPersonalRecordService personalRecordService, UserManager<ApplicationUser> userManager)
    {
        _exerciseService = exerciseService;
        _personalRecordService = personalRecordService;
        _userManager = userManager;
    }

    public Exercise? Exercise { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastPerformed { get; set; }
    public Set? BestSet { get; set; }
    public OneRepMaxEstimate BestSetOneRepMax { get; set; } = OneRepMaxEstimate.Empty;
    public List<PersonalRecord> PersonalRecords { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Exercise = await _exerciseService.GetExerciseAsync(id);

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
                var history = await _exerciseService.GetExerciseHistoryForUserAsync(id, userId);
                UsageCount = history.UsageCount;
                LastPerformed = history.LastPerformed;
                BestSet = history.BestSet;
                BestSetOneRepMax = BestSet != null
                    ? OneRepMaxCalculator.Calculate(BestSet.Weight ?? 0, BestSet.Reps ?? 0)
                    : OneRepMaxEstimate.Empty;
                PersonalRecords = await _personalRecordService.GetRecentRecordsForExerciseAsync(id, userId);
            }
        }

        return Page();
    }
}
