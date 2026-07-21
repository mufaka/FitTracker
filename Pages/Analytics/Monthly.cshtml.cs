using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Analytics;

[Authorize]
public class MonthlyModel : PageModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MonthlyModel(IAnalyticsService analyticsService, UserManager<ApplicationUser> userManager)
    {
        _analyticsService = analyticsService;
        _userManager = userManager;
    }

    public DateTime SelectedMonth { get; set; }
    public AnalyticsPeriodSummary Summary { get; set; } = new();
    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public async Task<IActionResult> OnGetAsync(string? month)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        if (!string.IsNullOrWhiteSpace(month) && DateTime.TryParse($"{month}-01", out var parsedMonth))
        {
            SelectedMonth = parsedMonth.Date;
        }
        else
        {
            SelectedMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        }

        Summary = await _analyticsService.GetMonthlySummaryAsync(userId, SelectedMonth);
        return Page();
    }
}
