using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Plans;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IWorkoutPlanService _planService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IWorkoutPlanService planService, UserManager<ApplicationUser> userManager)
    {
        _planService = planService;
        _userManager = userManager;
    }

    public List<WorkoutPlan> Plans { get; set; } = new();

    /// <summary>Labels the prescribed distances; the stored values are canonical kilometres.</summary>
    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        // Inactive plans come back too, so a retired plan can be brought back from here (WDM-16).
        Plans = await _planService.GetPlansAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostSetActiveAsync(int id, bool isActive)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        // The service owns the ownership predicate; false means it found nothing to act on,
        // which is all this page is entitled to know (WDM-SEC-06).
        if (!await _planService.SetPlanActiveAsync(id, userId, isActive))
            return NotFound();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (!await _planService.DeletePlanAsync(id, userId))
            return NotFound();

        return RedirectToPage();
    }
}
