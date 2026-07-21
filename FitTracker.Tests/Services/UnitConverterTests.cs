using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitTracker.Tests.Services;

public class UnitConverterTests
{
    [Fact]
    public async Task CanonicalStorage_LeavesVolumeRecordsAndOneRepMaxUnchangedWhenThePreferenceChanges()
    {
        // WDM-TEST-12. The defect this whole change exists to fix: switching lbs to kg used to
        // relabel history rather than convert it. Nothing derived from a stored weight may move.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var user = new ApplicationUser
        {
            Id = "user-units-1",
            UserName = "units@example.com",
            NormalizedUserName = "UNITS@EXAMPLE.COM",
            Email = "units@example.com",
            NormalizedEmail = "UNITS@EXAMPLE.COM",
            PreferredUnits = "lbs"
        };
        var exercise = new Exercise
        {
            Name = "Back Squat",
            Category = "Strength",
            Equipment = "Barbell",
            MuscleGroups = "Legs",
            TracksOneRepMax = true
        };

        context.Users.Add(user);
        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();

        var service = new WorkoutService(context);
        var workout = await service.StartWorkoutAsync(user.Id);
        Assert.True(await service.AddExerciseToWorkoutAsync(workout.Id, exercise.Id, user.Id));

        var workoutExerciseId = await context.WorkoutExercises
            .Where(we => we.WorkoutId == workout.Id)
            .Select(we => we.Id)
            .SingleAsync();

        // Entered as a lbs user would enter it.
        Assert.True(await service.LogSetAsync(workoutExerciseId, user.Id, 225m, 5, null));
        Assert.True((await service.CompleteWorkoutAsync(workout.Id, user.Id, null)).Succeeded);

        var volumeAsPounds = await service.CalculateWorkoutVolumeAsync(workout.Id, user.Id);
        var recordAsPounds = await context.PersonalRecords.AsNoTracking().SingleAsync(pr => pr.UserId == user.Id);

        // The user switches to kilograms. Only the presentation preference changes.
        var stored = await context.Users.SingleAsync(u => u.Id == user.Id);
        stored.PreferredUnits = "kg";
        await context.SaveChangesAsync();

        var volumeAsKilograms = await service.CalculateWorkoutVolumeAsync(workout.Id, user.Id);
        var recordAsKilograms = await context.PersonalRecords.AsNoTracking().SingleAsync(pr => pr.UserId == user.Id);

        Assert.Equal(volumeAsPounds, volumeAsKilograms);
        Assert.Equal(recordAsPounds.Weight, recordAsKilograms.Weight);
        Assert.Equal(recordAsPounds.OneRepMax, recordAsKilograms.OneRepMax);

        // And the stored figures really are canonical, not the numbers that were typed.
        Assert.Equal(225m, UnitConverter.ToDisplayWeight(recordAsPounds.Weight, "lbs"));
        Assert.NotEqual(225m, recordAsPounds.Weight);

        // What does change is what the user is shown — which is the entire point.
        Assert.NotEqual(
            UnitConverter.ToDisplayWeight(recordAsPounds.Weight, "lbs"),
            UnitConverter.ToDisplayWeight(recordAsPounds.Weight, "kg"));
    }

    [Fact]
    public void NormalizeWeightUnit_FallsBackToPoundsForAnythingUnrecognised()
    {
        Assert.Equal(UnitConverter.Pounds, UnitConverter.NormalizeWeightUnit(null));
        Assert.Equal(UnitConverter.Pounds, UnitConverter.NormalizeWeightUnit(""));
        Assert.Equal(UnitConverter.Pounds, UnitConverter.NormalizeWeightUnit("   "));
        Assert.Equal(UnitConverter.Pounds, UnitConverter.NormalizeWeightUnit("stone"));
        Assert.Equal(UnitConverter.Pounds, UnitConverter.NormalizeWeightUnit("LBS"));
    }

    [Theory]
    [InlineData("kg")]
    [InlineData("KG")]
    [InlineData(" kg ")]
    public void NormalizeWeightUnit_RecognisesKilogramsWhateverTheCasing(string stored)
    {
        Assert.Equal(UnitConverter.Kilograms, UnitConverter.NormalizeWeightUnit(stored));
    }

    [Fact]
    public void DistanceUnitFor_DerivesDistanceFromTheWeightPreference()
    {
        Assert.Equal(UnitConverter.Miles, UnitConverter.DistanceUnitFor(UnitConverter.Pounds));
        Assert.Equal(UnitConverter.Kilometres, UnitConverter.DistanceUnitFor(UnitConverter.Kilograms));
        Assert.Equal(UnitConverter.Miles, UnitConverter.DistanceUnitFor(null));
    }

    [Fact]
    public void ToCanonicalWeight_StoresKilogramsVerbatim()
    {
        Assert.Equal(100m, UnitConverter.ToCanonicalWeight(100m, "kg"));
        Assert.Equal(100m, UnitConverter.ToDisplayWeight(100m, "kg"));
    }

    [Fact]
    public void ToCanonicalWeight_ConvertsPoundsToKilograms()
    {
        // 45 lbs is the standard Olympic bar; four decimals is what keeps it stable on the way back.
        Assert.Equal(20.4117m, UnitConverter.ToCanonicalWeight(45m, "lbs"));
        Assert.Equal(45m, UnitConverter.ToDisplayWeight(20.4117m, "lbs"));
    }

    [Fact]
    public void ToDisplayWeight_ConvertsKilogramsToPounds()
    {
        Assert.Equal(220.46m, UnitConverter.ToDisplayWeight(100m, "lbs"));
    }

    [Theory]
    [InlineData("lbs")]
    [InlineData("kg")]
    public void ToCanonicalWeight_RoundTripsAtDisplayPrecision(string displayUnit)
    {
        // WDM-34: a weight displayed, then re-submitted unmodified, must not move the stored value.
        for (var displayed = 0.25m; displayed <= 700m; displayed += 0.25m)
        {
            var canonical = UnitConverter.ToCanonicalWeight(displayed, displayUnit);
            var roundTripped = UnitConverter.ToDisplayWeight(canonical, displayUnit);

            Assert.Equal(displayed, roundTripped);

            // And again, to catch drift that only shows up after repeated edits.
            Assert.Equal(canonical, UnitConverter.ToCanonicalWeight(roundTripped, displayUnit));
        }
    }

    [Theory]
    [InlineData("lbs")]
    [InlineData("kg")]
    public void ToCanonicalWeight_RoundTripsAwkwardTwoDecimalValues(string displayUnit)
    {
        foreach (var displayed in new[] { 0.01m, 0.99m, 1.01m, 12.34m, 45.67m, 99.99m, 137.53m, 315.05m })
        {
            var canonical = UnitConverter.ToCanonicalWeight(displayed, displayUnit);
            Assert.Equal(displayed, UnitConverter.ToDisplayWeight(canonical, displayUnit));
        }
    }

    [Fact]
    public void ToCanonicalDistance_ConvertsMilesToKilometres()
    {
        Assert.Equal(1.6093m, UnitConverter.ToCanonicalDistance(1m, "lbs"));
        Assert.Equal(1m, UnitConverter.ToDisplayDistance(1.6093m, "lbs"));
    }

    [Fact]
    public void ToCanonicalDistance_StoresKilometresVerbatimForMetricUsers()
    {
        Assert.Equal(5m, UnitConverter.ToCanonicalDistance(5m, "kg"));
        Assert.Equal(5m, UnitConverter.ToDisplayDistance(5m, "kg"));
    }

    [Fact]
    public void ToDisplayDistance_ShowsAFiveKilometreRunInMilesForImperialUsers()
    {
        Assert.Equal(3.11m, UnitConverter.ToDisplayDistance(5m, "lbs"));
    }

    [Theory]
    [InlineData("lbs")]
    [InlineData("kg")]
    public void ToCanonicalDistance_RoundTripsAtDisplayPrecision(string displayUnit)
    {
        for (var displayed = 0.01m; displayed <= 30m; displayed += 0.01m)
        {
            var canonical = UnitConverter.ToCanonicalDistance(displayed, displayUnit);
            Assert.Equal(displayed, UnitConverter.ToDisplayDistance(canonical, displayUnit));
        }
    }

    [Fact]
    public void ToCanonicalWeight_TreatsTheSamePhysicalLoadIdenticallyWhicheverUnitItWasTypedIn()
    {
        var typedInKilograms = UnitConverter.ToCanonicalWeight(100m, "kg");
        var typedInPounds = UnitConverter.ToCanonicalWeight(220.46m, "lbs");

        // The two canonical values differ in the fourth decimal — 220.46 lbs is what 100 kg
        // rounds to for display, not its exact equivalent. What matters is that the same bar
        // reads the same to either user, whichever unit it was entered in.
        Assert.Equal(
            UnitConverter.ToDisplayWeight(typedInKilograms, "kg"),
            UnitConverter.ToDisplayWeight(typedInPounds, "kg"));
        Assert.Equal(
            UnitConverter.ToDisplayWeight(typedInKilograms, "lbs"),
            UnitConverter.ToDisplayWeight(typedInPounds, "lbs"));
    }

    [Fact]
    public void ToCanonicalWeight_AndDistance_PassThroughNulls()
    {
        Assert.Null(UnitConverter.ToCanonicalWeight((decimal?)null, "lbs"));
        Assert.Null(UnitConverter.ToDisplayWeight((decimal?)null, "lbs"));
        Assert.Null(UnitConverter.ToCanonicalDistance((decimal?)null, "lbs"));
        Assert.Null(UnitConverter.ToDisplayDistance((decimal?)null, "lbs"));
    }

    [Fact]
    public void WeightIncrement_DiffersBetweenDisplayUnits()
    {
        // WDM-35: a 5 lb jump and a 5 kg jump are not the same increment. The same bar,
        // described in either unit, must not produce the same number.
        Assert.Equal(5m, UnitConverter.WeightIncrement(225m, "lbs"));
        Assert.Equal(2.5m, UnitConverter.WeightIncrement(102m, "kg"));

        Assert.Equal(2.5m, UnitConverter.WeightIncrement(40m, "lbs"));
        Assert.Equal(1.25m, UnitConverter.WeightIncrement(18m, "kg"));
    }

    [Fact]
    public void WeightIncrement_StepsUpAtTheThresholdForEachUnit()
    {
        Assert.Equal(2.5m, UnitConverter.WeightIncrement(49.99m, "lbs"));
        Assert.Equal(5m, UnitConverter.WeightIncrement(50m, "lbs"));

        Assert.Equal(1.25m, UnitConverter.WeightIncrement(24.99m, "kg"));
        Assert.Equal(2.5m, UnitConverter.WeightIncrement(25m, "kg"));
    }

    [Fact]
    public void FormatWeight_CarriesTheDisplayUnitAndDropsTrailingZeros()
    {
        Assert.Equal("45 lbs", UnitConverter.FormatWeight(20.4117m, "lbs"));
        Assert.Equal("100 kg", UnitConverter.FormatWeight(100m, "kg"));
        Assert.Equal("62.5 kg", UnitConverter.FormatWeight(62.5m, "kg"));
        Assert.Equal(string.Empty, UnitConverter.FormatWeight(null, "lbs"));
    }

    [Fact]
    public void FormatDistance_CarriesTheDerivedDistanceUnit()
    {
        Assert.Equal("3.11 mi", UnitConverter.FormatDistance(5m, "lbs"));
        Assert.Equal("5 km", UnitConverter.FormatDistance(5m, "kg"));
        Assert.Equal(string.Empty, UnitConverter.FormatDistance(null, "kg"));
    }

    [Fact]
    public void FormatVolume_ConvertsAWeightTimesRepsProduct()
    {
        // Volume is linear in weight, so the weight factor converts unchanged.
        Assert.Equal("2,205 lbs", UnitConverter.FormatVolume(1000m, "lbs"));
        Assert.Equal("1,000 kg", UnitConverter.FormatVolume(1000m, "kg"));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(0, "")]
    [InlineData(45, "45s")]
    [InlineData(90, "1:30")]
    [InlineData(600, "10:00")]
    [InlineData(3900, "1:05:00")]
    public void FormatDuration_RendersSecondsWithoutAUnitPreference(int? seconds, string expected)
    {
        Assert.Equal(expected, UnitConverter.FormatDuration(seconds));
    }
}
