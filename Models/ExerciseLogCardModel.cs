using FitTracker.Services;

namespace FitTracker.Models;

/// <summary>
/// One exercise as the logging screen draws it. The same card serves the full list and the guided
/// flow; <see cref="Focused"/> is what decides whether the status controls are the quiet row of chips
/// the list shows or the primary question the flow asks.
/// </summary>
public class ExerciseLogCardModel
{
    public WorkoutExercise WorkoutExercise { get; set; } = null!;

    /// <summary>Its place in the workout, 1-based, as the user sees it numbered.</summary>
    public int Position { get; set; }

    public string UserUnits { get; set; } = UnitConverter.DefaultWeightUnit;

    public bool IsWorkoutCompleted { get; set; }

    /// <summary>Whether this is the one exercise on screen rather than one card among many.</summary>
    public bool Focused { get; set; }

    public ProgressiveOverloadSuggestion? Suggestion { get; set; }

    /// <summary>
    /// The plan's prescription for this exercise, read live from the plan and never copied onto the
    /// workout. Null for an ad-hoc workout, and for anything the user added that the plan does not name.
    /// </summary>
    public WorkoutPlanExercise? Planned { get; set; }
}
