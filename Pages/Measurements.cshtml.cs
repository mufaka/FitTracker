using System.ComponentModel.DataAnnotations;
using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Pages;

[Authorize]
public class MeasurementsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MeasurementsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int? EditId { get; set; }

    [BindProperty]
    public MeasurementEditorModel Measurement { get; set; } = new();

    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    // Weights below are canonical kilograms, as stored; the view converts them once at render.
    public List<BodyMeasurement> Measurements { get; set; } = new();
    public decimal? LatestWeight { get; set; }
    public decimal? LatestBodyFatPercentage { get; set; }
    public DateTime? LatestMeasurementDate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        await LoadMeasurementsAsync(userId);

        if (EditId.HasValue)
        {
            var existingMeasurement = Measurements.FirstOrDefault(m => m.Id == EditId.Value);
            if (existingMeasurement == null)
                return NotFound();

            Measurement = new MeasurementEditorModel
            {
                Id = existingMeasurement.Id,
                Date = existingMeasurement.Date.Date,
                // The editor holds display values; the save handler converts back.
                Weight = UnitConverter.ToDisplayWeight(existingMeasurement.Weight, UserUnits),
                BodyFatPercentage = existingMeasurement.BodyFatPercentage,
                Chest = existingMeasurement.Chest,
                Waist = existingMeasurement.Waist,
                Arms = existingMeasurement.Arms,
                Legs = existingMeasurement.Legs,
                Notes = existingMeasurement.Notes
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userManager.GetUserAsync(User);
        UserUnits = UnitConverter.NormalizeWeightUnit(user?.PreferredUnits);

        ValidateMeasurement();
        if (!ModelState.IsValid)
        {
            await LoadMeasurementsAsync(userId);
            EditId = Measurement.Id;
            return Page();
        }

        if (Measurement.Id.HasValue)
        {
            var existingMeasurement = await _context.BodyMeasurements
                .FirstOrDefaultAsync(m => m.Id == Measurement.Id.Value && m.UserId == userId);

            if (existingMeasurement == null)
                return NotFound();

            existingMeasurement.Date = Measurement.Date.Date;
            existingMeasurement.Weight = UnitConverter.ToCanonicalWeight(Measurement.Weight, UserUnits);
            existingMeasurement.BodyFatPercentage = Measurement.BodyFatPercentage;
            existingMeasurement.Chest = Measurement.Chest;
            existingMeasurement.Waist = Measurement.Waist;
            existingMeasurement.Arms = Measurement.Arms;
            existingMeasurement.Legs = Measurement.Legs;
            existingMeasurement.Notes = string.IsNullOrWhiteSpace(Measurement.Notes) ? null : Measurement.Notes.Trim();
        }
        else
        {
            _context.BodyMeasurements.Add(new BodyMeasurement
            {
                UserId = userId,
                Date = Measurement.Date.Date,
                Weight = UnitConverter.ToCanonicalWeight(Measurement.Weight, UserUnits),
                BodyFatPercentage = Measurement.BodyFatPercentage,
                Chest = Measurement.Chest,
                Waist = Measurement.Waist,
                Arms = Measurement.Arms,
                Legs = Measurement.Legs,
                Notes = string.IsNullOrWhiteSpace(Measurement.Notes) ? null : Measurement.Notes.Trim()
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToPage("/Measurements");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var measurement = await _context.BodyMeasurements
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (measurement == null)
            return NotFound();

        _context.BodyMeasurements.Remove(measurement);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Measurements");
    }

    private async Task LoadMeasurementsAsync(string userId)
    {
        Measurements = await _context.BodyMeasurements
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        var latestMeasurement = Measurements.FirstOrDefault();
        LatestMeasurementDate = latestMeasurement?.Date;
        LatestWeight = Measurements.FirstOrDefault(m => m.Weight.HasValue)?.Weight;
        LatestBodyFatPercentage = Measurements.FirstOrDefault(m => m.BodyFatPercentage.HasValue)?.BodyFatPercentage;
    }

    private void ValidateMeasurement()
    {
        if (!Measurement.Weight.HasValue &&
            !Measurement.BodyFatPercentage.HasValue &&
            !Measurement.Chest.HasValue &&
            !Measurement.Waist.HasValue &&
            !Measurement.Arms.HasValue &&
            !Measurement.Legs.HasValue &&
            string.IsNullOrWhiteSpace(Measurement.Notes))
        {
            ModelState.AddModelError(string.Empty, "Enter at least one measurement value or note.");
        }

        if (Measurement.Date == default)
        {
            ModelState.AddModelError("Measurement.Date", "A measurement date is required.");
        }
    }
}

public class MeasurementEditorModel
{
    public int? Id { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Entered and shown in the user's display unit; the page converts to canonical kilograms on
    /// save. The bound is therefore read in whichever unit the user types, so it is a typo guard
    /// rather than a physiological limit — it has to clear the heaviest real body weight in the
    /// looser unit (2000 lbs is about 907 kg), which leaves it generous for a kg user.
    /// </summary>
    [Display(Name = "Weight")]
    [Range(0, 2000)]
    public decimal? Weight { get; set; }

    [Display(Name = "Body fat %")]
    [Range(0, 100)]
    public decimal? BodyFatPercentage { get; set; }

    [Range(0, 200)]
    public decimal? Chest { get; set; }

    [Range(0, 200)]
    public decimal? Waist { get; set; }

    [Range(0, 200)]
    public decimal? Arms { get; set; }

    [Range(0, 200)]
    public decimal? Legs { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
