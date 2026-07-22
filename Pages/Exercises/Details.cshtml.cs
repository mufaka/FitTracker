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
    private readonly IOneRepMaxService _oneRepMaxService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(
        IExerciseService exerciseService,
        IPersonalRecordService personalRecordService,
        IOneRepMaxService oneRepMaxService,
        UserManager<ApplicationUser> userManager)
    {
        _exerciseService = exerciseService;
        _personalRecordService = personalRecordService;
        _oneRepMaxService = oneRepMaxService;
        _userManager = userManager;
    }

    public Exercise? Exercise { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastPerformed { get; set; }
    public Set? BestSet { get; set; }
    public List<PersonalRecord> PersonalRecords { get; set; } = new();

    public bool TracksOneRepMax => Exercise?.TracksOneRepMax == true;

    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    /// <summary>The session that produced the best estimate, or null if none of the logged sets can.</summary>
    public OneRepMaxPoint? BestOneRepMax { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Exercise = await _exerciseService.GetExerciseAsync(id);

        if (Exercise == null)
        {
            return NotFound();
        }

        // The library is reference data and renders anonymously, so there may be no preference to
        // read: GetUserAsync returns null for a visitor and the default unit stands.
        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

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
                PersonalRecords = await _personalRecordService.GetRecentRecordsForExerciseAsync(id, userId);

                if (TracksOneRepMax)
                {
                    // The heaviest set is not necessarily the best estimate, so take it from the
                    // same history the tracking page reads rather than recalculating from BestSet.
                    var trend = await _oneRepMaxService.GetExerciseTrendAsync(userId, id);
                    BestOneRepMax = trend?.Points
                        .OrderByDescending(point => point.OneRepMax)
                        .FirstOrDefault();
                }
            }
        }

        return Page();
    }
}
