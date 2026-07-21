using FitTracker.Models;
using FitTracker.Services;
using Xunit;

namespace FitTracker.Tests.Services;

/// <summary>
/// How a prescription becomes input rows. Pure and database-free, like the model itself.
/// </summary>
public class SetInputModelTests
{
    [Fact]
    public void For_GivesOneRowPerPrescribedSet_PreFilledWithTheTargetReps()
    {
        var model = SetInputModel.For(Strength(), "kg", Planned(sets: 3, reps: 10));

        Assert.Equal(3, model.Rows.Count);
        Assert.Equal(new[] { 1, 2, 3 }, model.Rows.Select(r => r.SetNumber).ToArray());
        Assert.All(model.Rows, row => Assert.Equal(10, row.Reps));
    }

    [Fact]
    public void For_OffersOnlyTheSetsStillOutstanding()
    {
        // Two of the three are already recorded and shown in the table above; offering them again
        // would invite logging them twice.
        var exercise = Strength(loggedSets: 2);

        var model = SetInputModel.For(exercise, "kg", Planned(sets: 3, reps: 10));

        var row = Assert.Single(model.Rows);
        Assert.Equal(3, row.SetNumber);
    }

    [Fact]
    public void For_IdentifiesEveryRowByTheSetItWillBecome()
    {
        // The draft autosave keys off the input name, and the inputs are named by set number — so a
        // row means the same thing on every render. Numbering by position instead would call the row
        // for set 3 "row 0", which is the name the draft is holding set 1's weight under, and the
        // weight just logged would reappear in the next row unasked.
        var fresh = SetInputModel.For(Strength(), "kg", Planned(sets: 3, reps: 10));
        var partWay = SetInputModel.For(Strength(loggedSets: 2), "kg", Planned(sets: 3, reps: 10));

        Assert.Equal(new[] { 1, 2, 3 }, fresh.Rows.Select(r => r.SetNumber).ToArray());
        Assert.Equal(new[] { 3 }, partWay.Rows.Select(r => r.SetNumber).ToArray());

        // A set already recorded is never offered under a name again.
        Assert.DoesNotContain(partWay.Rows, row => row.SetNumber <= 2);
    }

    [Fact]
    public void For_AlwaysKeepsOneRowSoAnExtraSetStaysWithinReach()
    {
        // Past the prescription, and with no prescription at all.
        var beyond = SetInputModel.For(Strength(loggedSets: 5), "kg", Planned(sets: 3, reps: 10));
        Assert.Single(beyond.Rows);
        Assert.Equal(6, beyond.Rows[0].SetNumber);

        var adHoc = SetInputModel.For(Strength(loggedSets: 1), "kg");
        Assert.Single(adHoc.Rows);
        Assert.Equal(2, adHoc.Rows[0].SetNumber);
        Assert.Null(adHoc.Rows[0].Reps);
    }

    [Fact]
    public void For_ConvertsAPrescribedDistanceIntoTheUsersOwnUnit()
    {
        // Targets are stored canonically like every other measurement, and a row is form state —
        // display units in both directions. It arrives rounded for display, as every rendered
        // measurement is: the prefill is a convenience, and what gets recorded is what the user
        // submits.
        var planned = new WorkoutPlanExercise { TargetDistance = 5.0051m };

        var miles = SetInputModel.For(Cardio(), "lbs", planned);
        var kilometres = SetInputModel.For(Cardio(), "kg", planned);

        Assert.Equal(3.11m, miles.Rows[0].Distance);
        Assert.Equal(5.01m, kilometres.Rows[0].Distance);
    }

    [Fact]
    public void For_StillShowsOnlyTheInputsTheMovementCanUse()
    {
        // WDM-UI-09 unchanged by the move to rows.
        var lift = SetInputModel.For(Strength(), "kg", Planned(sets: 2, reps: 5));
        Assert.True(lift.ShowWeight && lift.ShowReps);
        Assert.False(lift.ShowDuration || lift.ShowDistance);

        var run = SetInputModel.For(Cardio(), "kg");
        Assert.True(run.ShowDuration && run.ShowDistance);
        Assert.False(run.ShowWeight || run.ShowReps);
    }

    [Fact]
    public void For_NumbersRowsPastTheHighestRecordedSetNotPastTheCount()
    {
        // Removing a set leaves a gap rather than renumbering what is left, so counting would offer
        // a number that is already sitting in the table above — and the draft autosave, which keys
        // off the set number, would pour the removed set's weight into it.
        var exercise = Strength(loggedSets: 3);
        exercise.Sets.Remove(exercise.Sets.First(set => set.SetNumber == 1));

        var model = SetInputModel.For(exercise, "kg", Planned(sets: 3, reps: 10));

        var row = Assert.Single(model.Rows);
        Assert.Equal(4, row.SetNumber);
    }

    private static WorkoutPlanExercise Planned(int? sets, int? reps) =>
        new() { TargetSets = sets, TargetReps = reps };

    private static WorkoutExercise Strength(int loggedSets = 0) =>
        Build("Strength", loggedSets);

    private static WorkoutExercise Cardio(int loggedSets = 0) =>
        Build("Cardio", loggedSets);

    private static WorkoutExercise Build(string category, int loggedSets)
    {
        var workoutExercise = new WorkoutExercise
        {
            Id = 7,
            Exercise = new Exercise { Name = "Movement", Category = category, Equipment = "Barbell", MuscleGroups = "Legs" }
        };

        for (var setNumber = 1; setNumber <= loggedSets; setNumber++)
            workoutExercise.Sets.Add(new Set { SetNumber = setNumber, Weight = 60m, Reps = 5 });

        return workoutExercise;
    }
}
