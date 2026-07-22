using FitTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Services;

/// <summary>
/// Resolves the unit a user reads and enters measurements in.
///
/// Kept apart from <see cref="UnitConverter"/> so that the converter stays pure and directly
/// testable without a database (WDM-NF-06). Services that build prose, a CSV or a PDF — anything
/// with no view to convert on their behalf — look the preference up through here rather than
/// trusting a caller to pass the right one.
/// </summary>
public static class DisplayUnits
{
    public static async Task<string> ForUserAsync(ApplicationDbContext context, string userId)
    {
        var preferred = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferredUnits)
            .FirstOrDefaultAsync();

        return UnitConverter.NormalizeWeightUnit(preferred);
    }
}
