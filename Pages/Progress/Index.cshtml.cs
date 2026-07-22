using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Progress;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IAnalyticsService analyticsService, UserManager<ApplicationUser> userManager)
    {
        _analyticsService = analyticsService;
        _userManager = userManager;
    }

    public OverallProgressSummary Summary { get; set; } = new();

    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        Summary = await _analyticsService.GetOverallProgressAsync(userId);
        return Page();
    }
}
