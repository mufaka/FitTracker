using FitTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages;

public class OneRepMaxModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public decimal? Weight { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Reps { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Units { get; set; } = "lbs";

    public OneRepMaxEstimate Estimate { get; set; } = OneRepMaxEstimate.Empty;

    public void OnGet()
    {
        CalculateEstimate();
    }

    public void OnPost()
    {
        CalculateEstimate();
    }

    private void CalculateEstimate()
    {
        Units = string.Equals(Units, "kg", StringComparison.OrdinalIgnoreCase) ? "kg" : "lbs";
        Estimate = Weight.HasValue && Reps.HasValue
            ? OneRepMaxCalculator.Calculate(Weight.Value, Reps.Value)
            : OneRepMaxEstimate.Empty;
    }
}
