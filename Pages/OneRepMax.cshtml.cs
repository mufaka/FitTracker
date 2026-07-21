using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages;

public class OneRepMaxModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public OneRepMaxModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public decimal? Weight { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Reps { get; set; }

    /// <summary>
    /// Null until resolved. Nothing here is persisted, so this is a label rather than a conversion —
    /// but a signed-in user who lands from the dashboard and types 100 must not be told the answer is
    /// in pounds when they think in kilograms. An explicit <c>?units=</c> still wins, which is how the
    /// link from an exercise's best set arrives already carrying the unit it was rendered in.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Units { get; set; }

    public OneRepMaxEstimate Estimate { get; set; } = OneRepMaxEstimate.Empty;

    /// <summary>A complete set was entered but it cannot produce an estimate — worth saying so rather
    /// than showing the same prompt as an empty form.</summary>
    public bool IsOutOfRange { get; set; }

    public Task OnGetAsync() => CalculateEstimateAsync();

    public Task OnPostAsync() => CalculateEstimateAsync();

    private async Task CalculateEstimateAsync()
    {
        // The page is anonymous by design, so there may be no user to ask.
        Units = UnitConverter.NormalizeWeightUnit(
            Units ?? (await _userManager.GetUserAsync(User))?.PreferredUnits);

        if (!Weight.HasValue || !Reps.HasValue)
        {
            Estimate = OneRepMaxEstimate.Empty;
            IsOutOfRange = false;
            return;
        }

        Estimate = OneRepMaxCalculator.Calculate(Weight.Value, Reps.Value);
        IsOutOfRange = !Estimate.HasValue;
    }
}
