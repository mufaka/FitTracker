using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Analytics;

[Authorize]
public class WeeklyModel : PageModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly UserManager<ApplicationUser> _userManager;

    public WeeklyModel(IAnalyticsService analyticsService, UserManager<ApplicationUser> userManager)
    {
        _analyticsService = analyticsService;
        _userManager = userManager;
    }

    public DateTime SelectedWeek { get; set; }
    public AnalyticsPeriodSummary Summary { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? week)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        SelectedWeek = DateTime.TryParse(week, out var parsedWeek)
            ? parsedWeek.Date
            : DateTime.UtcNow.Date;

        Summary = await _analyticsService.GetWeeklySummaryAsync(userId, SelectedWeek);
        return Page();
    }
}
