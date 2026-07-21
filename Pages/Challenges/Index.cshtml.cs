using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Challenges;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IChallengeService _challengeService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IChallengeService challengeService, UserManager<ApplicationUser> userManager)
    {
        _challengeService = challengeService;
        _userManager = userManager;
    }

    public ChallengeOverviewSummary Summary { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        Summary = await _challengeService.GetChallengeOverviewAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostJoinAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        // Joining evaluates immediately so a goal already met inside the window
        // does not sit unfinished until the next completed workout.
        if (await _challengeService.JoinChallengeAsync(userId, id))
        {
            await _challengeService.EvaluateChallengesAsync(userId);
            StatusMessage = "Challenge started. The clock runs from today.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLeaveAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (await _challengeService.LeaveChallengeAsync(userId, id))
        {
            StatusMessage = "Left the challenge.";
        }

        return RedirectToPage();
    }
}
