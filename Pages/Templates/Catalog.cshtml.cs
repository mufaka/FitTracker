using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Templates;

/// <summary>
/// The built-in catalog, readable without an account (WDM-45). Deliberately a separate page rather
/// than relaxed authorization on <c>/Templates/Index</c>, which shows a user's own templates and
/// must stay protected (D5).
///
/// No <c>[Authorize]</c>, and the only data it reads is <c>GetCatalogAsync</c>, which returns
/// ownerless templates and nothing else (WDM-SEC-05).
/// </summary>
public class CatalogModel : PageModel
{
    private readonly ITemplateService _templateService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CatalogModel(ITemplateService templateService, UserManager<ApplicationUser> userManager)
    {
        _templateService = templateService;
        _userManager = userManager;
    }

    public List<WorkoutTemplate> Templates { get; set; } = new();

    /// <summary>Falls back to the default when nobody is signed in — there is no preference to read.</summary>
    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public bool IsSignedIn { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        IsSignedIn = user != null;
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        Templates = await _templateService.GetCatalogAsync();
    }
}
