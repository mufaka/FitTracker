using System.ComponentModel.DataAnnotations;
using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages;

[Authorize]
public class ExportModel : PageModel
{
    private readonly IExportService _exportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExportModel(IExportService exportService, UserManager<ApplicationUser> userManager)
    {
        _exportService = exportService;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public bool HasDateFilter => StartDate.HasValue || EndDate.HasValue;

    public IActionResult OnGet()
    {
        if (HasInvalidDateRange())
        {
            ModelState.AddModelError(string.Empty, "Start date must be on or before end date.");
        }

        return Page();
    }

    public async Task<IActionResult> OnGetWorkoutsCsvAsync()
    {
        var invalidDateResult = GetInvalidDateRangeResult();
        if (invalidDateResult != null)
            return invalidDateResult;

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var export = await _exportService.ExportWorkoutsCsvAsync(userId, StartDate, EndDate);
        return File(export.Content, export.ContentType, export.FileName);
    }

    public async Task<IActionResult> OnGetWorkoutsJsonAsync()
    {
        var invalidDateResult = GetInvalidDateRangeResult();
        if (invalidDateResult != null)
            return invalidDateResult;

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var export = await _exportService.ExportWorkoutsJsonAsync(userId, StartDate, EndDate);
        return File(export.Content, export.ContentType, export.FileName);
    }

    public async Task<IActionResult> OnGetMeasurementsCsvAsync()
    {
        var invalidDateResult = GetInvalidDateRangeResult();
        if (invalidDateResult != null)
            return invalidDateResult;

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var export = await _exportService.ExportMeasurementsCsvAsync(userId, StartDate, EndDate);
        return File(export.Content, export.ContentType, export.FileName);
    }

    public async Task<IActionResult> OnGetPersonalRecordsCsvAsync()
    {
        var invalidDateResult = GetInvalidDateRangeResult();
        if (invalidDateResult != null)
            return invalidDateResult;

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var export = await _exportService.ExportPersonalRecordsCsvAsync(userId, StartDate, EndDate);
        return File(export.Content, export.ContentType, export.FileName);
    }

    private bool HasInvalidDateRange()
    {
        return StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date;
    }

    private IActionResult? GetInvalidDateRangeResult()
    {
        return HasInvalidDateRange()
            ? BadRequest("Start date must be on or before end date.")
            : null;
    }
}
