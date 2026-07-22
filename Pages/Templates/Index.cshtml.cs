using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Templates;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ITemplateService _templateService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ITemplateService templateService, UserManager<ApplicationUser> userManager)
    {
        _templateService = templateService;
        _userManager = userManager;
    }

    public List<WorkoutTemplate> Templates { get; set; } = new();

    /// <summary>Which templates to show: personal, built-in, or all (WDM-09).</summary>
    [BindProperty(SupportsGet = true)]
    public string Ownership { get; set; } = TemplateOwnership.All;

    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        Ownership = Normalize(Ownership);

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        Templates = await _templateService.GetTemplatesAsync(userId, Ownership);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await _templateService.DeleteTemplateAsync(id, userId);
        return RedirectToPage(new { Ownership });
    }

    public async Task<IActionResult> OnPostCopyAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var copyId = await _templateService.CopyTemplateAsync(id, userId);
        if (!copyId.HasValue)
            return NotFound();

        // Straight into the copy: the reason to copy a built-in is to change it.
        return RedirectToPage("/Templates/Create", new { id = copyId.Value });
    }

    /// <summary>A filter arriving from the query string is free text until it is checked.</summary>
    private static string Normalize(string? ownership) => ownership switch
    {
        TemplateOwnership.Personal => TemplateOwnership.Personal,
        TemplateOwnership.BuiltIn => TemplateOwnership.BuiltIn,
        _ => TemplateOwnership.All
    };
}
