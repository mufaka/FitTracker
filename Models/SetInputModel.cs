namespace FitTracker.Models;

public class SetInputModel
{
    public int WorkoutExerciseId { get; set; }
    public int NextSetNumber { get; set; }
    public string UserUnits { get; set; } = "lbs";
}
