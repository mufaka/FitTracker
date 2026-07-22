using System.Globalization;

namespace FitTracker.Services;

/// <summary>
/// The one place canonical storage meets a user's display units (WDM-30 – WDM-35).
///
/// Weight is persisted in kilograms and distance in kilometres whatever the user prefers to see,
/// so that every comparison, aggregate and personal record is unit-safe by construction. Nothing
/// else in the application converts or formats a stored measurement: a second conversion site is
/// how a plausible-looking number ends up in the wrong unit.
///
/// Distance units are derived from the existing weight preference rather than stored separately —
/// <c>lbs</c> implies miles, <c>kg</c> implies kilometres.
/// </summary>
public static class UnitConverter
{
    public const string Pounds = "lbs";
    public const string Kilograms = "kg";
    public const string Miles = "mi";
    public const string Kilometres = "km";

    /// <summary>The unit an account is created with, and the fallback for a blank preference.</summary>
    public const string DefaultWeightUnit = Pounds;

    /// <summary>Decimal places a canonical value is stored to; matches <c>HasPrecision(10, 4)</c>.</summary>
    public const int CanonicalDecimals = 4;

    /// <summary>Decimal places a value is rendered and entered at.</summary>
    public const int DisplayDecimals = 2;

    private const decimal PoundsPerKilogram = 2.20462262185m;
    private const decimal KilometresPerMile = 1.609344m;

    /// <summary>
    /// Coerces any stored or posted preference to a unit this component understands. A null,
    /// blank or unrecognised value is treated as <see cref="DefaultWeightUnit"/> rather than
    /// throwing, because the preference is free text on the user record.
    /// </summary>
    public static string NormalizeWeightUnit(string? displayUnit) =>
        string.Equals(displayUnit?.Trim(), Kilograms, StringComparison.OrdinalIgnoreCase)
            ? Kilograms
            : Pounds;

    /// <summary>Distance unit implied by a weight preference (WDM §5.4).</summary>
    public static string DistanceUnitFor(string? displayUnit) =>
        NormalizeWeightUnit(displayUnit) == Kilograms ? Kilometres : Miles;

    // ---- Weight -------------------------------------------------------------------------

    /// <summary>Display unit → canonical kilograms.</summary>
    public static decimal ToCanonicalWeight(decimal value, string? displayUnit) =>
        NormalizeWeightUnit(displayUnit) == Kilograms
            ? RoundCanonical(value)
            : RoundCanonical(value / PoundsPerKilogram);

    /// <summary>Canonical kilograms → display unit.</summary>
    public static decimal ToDisplayWeight(decimal value, string? displayUnit) =>
        NormalizeWeightUnit(displayUnit) == Kilograms
            ? RoundDisplay(value)
            : RoundDisplay(value * PoundsPerKilogram);

    public static decimal? ToCanonicalWeight(decimal? value, string? displayUnit) =>
        value.HasValue ? ToCanonicalWeight(value.Value, displayUnit) : null;

    public static decimal? ToDisplayWeight(decimal? value, string? displayUnit) =>
        value.HasValue ? ToDisplayWeight(value.Value, displayUnit) : null;

    /// <summary>
    /// Converts a value that is a weight multiplied by a dimensionless count — training volume,
    /// or a sum of weights. Conversion is linear, so the weight factor applies unchanged; this
    /// overload exists so that call sites read as what they are rather than as a lone weight.
    /// </summary>
    public static decimal ToDisplayVolume(decimal canonicalVolume, string? displayUnit) =>
        ToDisplayWeight(canonicalVolume, displayUnit);

    // ---- Distance -----------------------------------------------------------------------

    /// <summary>Display unit → canonical kilometres.</summary>
    public static decimal ToCanonicalDistance(decimal value, string? displayUnit) =>
        DistanceUnitFor(displayUnit) == Kilometres
            ? RoundCanonical(value)
            : RoundCanonical(value * KilometresPerMile);

    /// <summary>Canonical kilometres → display unit.</summary>
    public static decimal ToDisplayDistance(decimal value, string? displayUnit) =>
        DistanceUnitFor(displayUnit) == Kilometres
            ? RoundDisplay(value)
            : RoundDisplay(value / KilometresPerMile);

    public static decimal? ToCanonicalDistance(decimal? value, string? displayUnit) =>
        value.HasValue ? ToCanonicalDistance(value.Value, displayUnit) : null;

    public static decimal? ToDisplayDistance(decimal? value, string? displayUnit) =>
        value.HasValue ? ToDisplayDistance(value.Value, displayUnit) : null;

    // ---- Progressive overload -----------------------------------------------------------

    /// <summary>
    /// The next sensible jump for a lift, in the user's display unit (WDM-35). The thresholds are
    /// per unit and reflect the plates that actually exist: a 5 lb jump and a 5 kg jump are not the
    /// same increment, and converting one from the other lands on a weight nobody can load.
    /// </summary>
    public static decimal WeightIncrement(decimal displayWeight, string? displayUnit) =>
        NormalizeWeightUnit(displayUnit) == Kilograms
            ? (displayWeight < 25m ? 1.25m : 2.5m)
            : (displayWeight < 50m ? 2.5m : 5m);

    // ---- Formatting ---------------------------------------------------------------------

    /// <summary>Unit label for a weight, for use next to a rendered value or on an input (WDM-UI-10).</summary>
    public static string WeightUnitLabel(string? displayUnit) => NormalizeWeightUnit(displayUnit);

    /// <summary>Unit label for a distance (WDM-UI-10).</summary>
    public static string DistanceUnitLabel(string? displayUnit) => DistanceUnitFor(displayUnit);

    /// <summary>
    /// Canonical weight → a display string with its unit, e.g. <c>"225 lbs"</c>. Returns an empty
    /// string for a missing value so a view can render it unconditionally.
    /// </summary>
    public static string FormatWeight(decimal? canonicalWeight, string? displayUnit) =>
        canonicalWeight.HasValue
            ? $"{Trim(ToDisplayWeight(canonicalWeight.Value, displayUnit))} {WeightUnitLabel(displayUnit)}"
            : string.Empty;

    /// <summary>
    /// Canonical weight → a bare display number, for an input value, a chart axis or a route value.
    /// Formatted invariantly, because those are machine-read: a comma decimal separator would break
    /// an HTML5 number input and silently truncate a query-string value.
    /// </summary>
    public static string FormatWeightValue(decimal? canonicalWeight, string? displayUnit) =>
        canonicalWeight.HasValue ? TrimInvariant(ToDisplayWeight(canonicalWeight.Value, displayUnit)) : string.Empty;

    /// <summary>Canonical volume → a display string with its unit.</summary>
    public static string FormatVolume(decimal canonicalVolume, string? displayUnit) =>
        $"{ToDisplayVolume(canonicalVolume, displayUnit):N0} {WeightUnitLabel(displayUnit)}";

    /// <summary>Canonical distance → a display string with its unit, e.g. <c>"3.11 mi"</c>.</summary>
    public static string FormatDistance(decimal? canonicalDistance, string? displayUnit) =>
        canonicalDistance.HasValue
            ? $"{Trim(ToDisplayDistance(canonicalDistance.Value, displayUnit))} {DistanceUnitLabel(displayUnit)}"
            : string.Empty;

    /// <summary>Canonical distance → a bare display number, invariant for the same reason.</summary>
    public static string FormatDistanceValue(decimal? canonicalDistance, string? displayUnit) =>
        canonicalDistance.HasValue ? TrimInvariant(ToDisplayDistance(canonicalDistance.Value, displayUnit)) : string.Empty;

    /// <summary>
    /// Seconds → <c>"45s"</c>, <c>"1:30"</c> or <c>"1:05:00"</c>. Duration is unit-independent
    /// (WDM-37); it lives here so that no view formats a stored measurement itself.
    /// </summary>
    public static string FormatDuration(int? seconds)
    {
        if (!seconds.HasValue || seconds.Value <= 0)
            return string.Empty;

        var span = TimeSpan.FromSeconds(seconds.Value);

        if (span.TotalMinutes < 1)
            return $"{span.Seconds}s";

        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{(int)span.TotalMinutes}:{span.Seconds:00}";
    }

    // ---- Rounding -----------------------------------------------------------------------

    /// <summary>
    /// Canonical values keep four decimals so that a display-precision round trip is stable
    /// (WDM-34, D2). At two decimals 45 lbs stores as 20.41 kg and comes back as 44.99 lbs.
    /// </summary>
    private static decimal RoundCanonical(decimal value) =>
        decimal.Round(value, CanonicalDecimals, MidpointRounding.AwayFromZero);

    private static decimal RoundDisplay(decimal value) =>
        decimal.Round(value, DisplayDecimals, MidpointRounding.AwayFromZero);

    /// <summary>Renders a display value without trailing zeros: 20.50 → "20.5", 20.00 → "20".</summary>
    private static string Trim(decimal value) => value.ToString("0.##");

    /// <summary>As <see cref="Trim"/>, for values something other than a human will read.</summary>
    private static string TrimInvariant(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
