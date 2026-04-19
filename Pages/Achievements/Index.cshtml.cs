using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Achievements;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAchievementService _achievementService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IAchievementService achievementService, UserManager<ApplicationUser> userManager)
    {
        _achievementService = achievementService;
        _userManager = userManager;
    }

    public AchievementOverviewSummary Summary { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        Summary = await _achievementService.GetAchievementOverviewAsync(userId);
        return Page();
    }
}
