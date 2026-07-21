using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Progress;

[Authorize]
public class OneRepMaxModel : PageModel
{
    private readonly IOneRepMaxService _oneRepMaxService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OneRepMaxModel(IOneRepMaxService oneRepMaxService, UserManager<ApplicationUser> userManager)
    {
        _oneRepMaxService = oneRepMaxService;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int? ExerciseId { get; set; }

    public OneRepMaxLeaderboard Leaderboard { get; set; } = new();

    public OneRepMaxTrend? Trend { get; set; }

    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        Leaderboard = await _oneRepMaxService.GetLeaderboardAsync(userId);

        // Without an explicit pick, open on the strongest lift rather than an empty panel.
        var selectedId = ExerciseId ?? Leaderboard.Entries.FirstOrDefault()?.ExerciseId;
        if (selectedId == null)
            return Page();

        Trend = await _oneRepMaxService.GetExerciseTrendAsync(userId, selectedId.Value);

        // An explicit id that resolves to nothing is a bad URL; falling back silently would hide it.
        if (Trend == null && ExerciseId.HasValue)
            return NotFound();

        return Page();
    }
}
