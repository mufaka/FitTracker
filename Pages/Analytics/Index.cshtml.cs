using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Analytics;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IAnalyticsPdfExportService _analyticsPdfExportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IAnalyticsService analyticsService, IAnalyticsPdfExportService analyticsPdfExportService, UserManager<ApplicationUser> userManager)
    {
        _analyticsService = analyticsService;
        _analyticsPdfExportService = analyticsPdfExportService;
        _userManager = userManager;
    }

    public AdvancedAnalyticsSummary Summary { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        Summary = await _analyticsService.GetAdvancedDashboardAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnGetExportPdfAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        Summary = await _analyticsService.GetAdvancedDashboardAsync(userId);
        var pdfBytes = _analyticsPdfExportService.ExportDashboardPdf(Summary);
        var fileName = $"fittracker-analytics-{Summary.RangeStart:yyyyMMdd}-{Summary.RangeEnd:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}
