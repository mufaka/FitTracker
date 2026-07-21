namespace FitTracker.Services;

/// <summary>
/// Estimates a one-rep max from a single working set, following
/// <c>Specifications/1RM_Calculation.md</c>: the average of Epley, Brzycki and
/// Lombardi, rounded to two decimal places.
/// </summary>
public static class OneRepMaxCalculator
{
    /// <summary>Fewest reps an estimate may be derived from.</summary>
    public const int MinimumEstimateReps = 3;

    /// <summary>Most reps an estimate may be derived from.</summary>
    public const int MaximumEstimateReps = 10;

    /// <summary>
    /// Whether a set can produce a 1RM at all. The formulas are only fitted for
    /// <see cref="MinimumEstimateReps"/>–<see cref="MaximumEstimateReps"/> reps; a single is not an
    /// estimate but a measured max, so it counts at face value. Everything else — doubles, high-rep
    /// sets, and anything without a weight — produces nothing rather than a number nobody should act on.
    /// </summary>
    public static bool IsEligible(decimal weight, int reps) =>
        weight > 0 && (reps == 1 || (reps >= MinimumEstimateReps && reps <= MaximumEstimateReps));

    public static OneRepMaxEstimate Calculate(decimal weight, int reps)
    {
        if (!IsEligible(weight, reps))
            return OneRepMaxEstimate.Empty;

        if (reps == 1)
        {
            var measured = Round(weight);
            return new OneRepMaxEstimate(measured, measured, measured, measured);
        }

        var epley = Epley(weight, reps);
        var brzycki = Brzycki(weight, reps);
        var lombardi = Lombardi(weight, reps);

        // Average the unrounded formulas and round once, so the blended figure does not
        // inherit three separate rounding errors.
        return new OneRepMaxEstimate(
            Round(epley),
            Round(brzycki),
            Round(lombardi),
            Round((epley + brzycki + lombardi) / 3m));
    }

    public static decimal CalculateAverage(decimal weight, int reps) => Calculate(weight, reps).Average;

    private static decimal Epley(decimal weight, int reps) => weight * (1 + (reps / 30m));

    private static decimal Brzycki(decimal weight, int reps) => weight * 36m / (37m - reps);

    private static decimal Lombardi(decimal weight, int reps) => weight * (decimal)Math.Pow(reps, 0.10);

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record OneRepMaxEstimate(decimal Epley, decimal Brzycki, decimal Lombardi, decimal Average)
{
    public static OneRepMaxEstimate Empty { get; } = new(0, 0, 0, 0);

    public bool HasValue => Average > 0;
}
