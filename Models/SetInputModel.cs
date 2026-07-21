using FitTracker.Services;

namespace FitTracker.Models;

/// <summary>
/// One row of the set logger: the inputs for a single set, carrying whatever the plan prescribed for
/// it as a starting value.
/// </summary>
public class SetInputRow
{
    /// <summary>
    /// The set number it will be recorded as, continuing from what is already logged — and what names
    /// its inputs. Deliberately not the row's position: rows renumber from zero on every render, so
    /// position-named inputs collide across renders and the draft autosave restores the weight from
    /// the set just logged into the row for the next one.
    /// </summary>
    public int SetNumber { get; set; }

    // Prescribed starting values, in display units — these are form state, not query results.
    public int? Reps { get; set; }
    public int? DurationSeconds { get; set; }
    public decimal? Distance { get; set; }
}

public class SetInputModel
{
    public int WorkoutExerciseId { get; set; }

    /// <summary>
    /// The unit the weight input is labelled with and entered in. It travels on the model because
    /// a partial has no page model to read the preference from.
    /// </summary>
    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    // Which inputs this movement can actually use (WDM-UI-09). A run has no reps and a bench press
    // has no distance; showing every field for every exercise makes the common case slower for
    // everybody. Every field stays optional, so a hidden one is simply left unrecorded.
    public bool ShowWeight { get; set; } = true;
    public bool ShowReps { get; set; } = true;
    public bool ShowDuration { get; set; }
    public bool ShowDistance { get; set; }

    /// <summary>
    /// A row per set still expected, so a prescribed 3 × 10 is three rows filled in and submitted
    /// once rather than the same form posted three times. Always at least one, so an exercise with
    /// no prescription — or one already past its prescription — can still record another set.
    /// </summary>
    public List<SetInputRow> Rows { get; set; } = new();

    /// <summary>
    /// The sets already recorded, in order. They share the table with the rows still on offer: one
    /// set per line, recorded and outstanding alike, rather than a table and a form stacked as two
    /// separate boxes inside the exercise card.
    /// </summary>
    public List<Set> LoggedSets { get; set; } = new();

    /// <summary>Whether the workout can still be edited; a completed one is the same table, read-only.</summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>How many measurement columns the table carries, which is what sizes them.</summary>
    public int FieldCount =>
        (ShowWeight ? 1 : 0) + (ShowReps ? 1 : 0) + (ShowDuration ? 1 : 0) + (ShowDistance ? 1 : 0) + 1;

    private const string Cardio = "Cardio";
    private const string Core = "Core";
    private const string Mobility = "Mobility";

    /// <summary>
    /// Derives the relevant inputs from the exercise's category — the only signal the library
    /// carries that is reliable enough to act on. Anything unrecognised falls back to the
    /// weight-and-reps shape the app has always shown.
    /// </summary>
    public static SetInputModel For(
        WorkoutExercise workoutExercise,
        string userUnits,
        WorkoutPlanExercise? planned = null,
        bool isEditable = true)
    {
        var category = workoutExercise.Exercise.Category;
        var isCardio = string.Equals(category, Cardio, StringComparison.OrdinalIgnoreCase);
        var isTimed = isCardio
            || string.Equals(category, Core, StringComparison.OrdinalIgnoreCase)
            || string.Equals(category, Mobility, StringComparison.OrdinalIgnoreCase);

        var logged = workoutExercise.Sets.Count;

        // Past the highest number recorded rather than past the count, matching how the service
        // numbers a new set: removing one leaves a gap, and counting would offer a number already in
        // the table above.
        var next = logged == 0 ? 1 : workoutExercise.Sets.Max(set => set.SetNumber) + 1;

        // Only what is left of the prescription; the sets already recorded are shown in the table
        // above, not offered for entry again. TargetSets is bounded to 20 on the plan, so this
        // cannot run away. One row minimum keeps an extra set always within reach.
        var outstanding = Math.Max(1, (planned?.TargetSets ?? 0) - logged);

        var rows = Enumerable.Range(0, outstanding)
            .Select(index => new SetInputRow
            {
                SetNumber = next + index,
                Reps = planned?.TargetReps,
                DurationSeconds = planned?.TargetDurationSeconds,
                // Stored canonically like every other measurement, so it converts on the way out.
                Distance = UnitConverter.ToDisplayDistance(planned?.TargetDistance, userUnits)
            })
            .ToList();

        return new SetInputModel
        {
            WorkoutExerciseId = workoutExercise.Id,
            UserUnits = userUnits,
            ShowWeight = !isCardio,
            ShowReps = !isCardio,
            ShowDuration = isTimed,
            ShowDistance = isCardio,
            // A completed workout has nothing left to offer, so it is the recorded sets alone.
            Rows = isEditable ? rows : new List<SetInputRow>(),
            LoggedSets = workoutExercise.Sets.OrderBy(set => set.SetNumber).ToList(),
            IsEditable = isEditable
        };
    }
}
