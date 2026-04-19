namespace FitTracker.Services;

public static class OneRepMaxCalculator
{
    public static OneRepMaxEstimate Calculate(decimal weight, int reps)
    {
        if (weight <= 0 || reps <= 0)
            return OneRepMaxEstimate.Empty;

        if (reps == 1)
        {
            var single = decimal.Round(weight, 2, MidpointRounding.AwayFromZero);
            return new OneRepMaxEstimate(single, single, single);
        }

        var epley = CalculateEpley(weight, reps);
        var brzycki = CalculateBrzycki(weight, reps);
        var validEstimates = new[] { epley, brzycki }.Where(value => value > 0).ToList();
        var average = validEstimates.Count == 0
            ? 0
            : decimal.Round(validEstimates.Average(), 2, MidpointRounding.AwayFromZero);

        return new OneRepMaxEstimate(epley, brzycki, average);
    }

    public static decimal CalculateAverage(decimal weight, int reps) => Calculate(weight, reps).Average;

    public static decimal CalculateEpley(decimal weight, int reps)
    {
        if (weight <= 0 || reps <= 0)
            return 0;

        return decimal.Round(weight * (1 + (reps / 30m)), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateBrzycki(decimal weight, int reps)
    {
        if (weight <= 0 || reps <= 0 || reps >= 37)
            return 0;

        return decimal.Round(weight * 36m / (37m - reps), 2, MidpointRounding.AwayFromZero);
    }
}

public sealed record OneRepMaxEstimate(decimal Epley, decimal Brzycki, decimal Average)
{
    public static OneRepMaxEstimate Empty { get; } = new(0, 0, 0);

    public bool HasValue => Average > 0;
}
