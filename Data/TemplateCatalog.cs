namespace FitTracker.Data;

/// <summary>
/// The 25 built-in templates the application ships with (WDM-40), defined as data so the seeding
/// logic in <see cref="DbInitializer"/> stays about mechanism rather than content.
///
/// Exercises are named, not identified: names are stable and readable, ids are neither. A name that
/// does not resolve fails startup rather than silently seeding a template with a hole in it
/// (WDM-43). Distances are canonical kilometres.
///
/// These are blocks, not sessions. A user combines them into a plan and performs the plan.
/// </summary>
public static class TemplateCatalog
{
    public sealed record Entry(string CatalogKey, string Name, string Description, CatalogExercise[] Exercises);

    public sealed record CatalogExercise(
        string ExerciseName,
        int? Sets = null,
        int? Reps = null,
        int? DurationSeconds = null,
        decimal? DistanceKm = null,
        string? Notes = null);

    public static IReadOnlyList<Entry> Entries { get; } = new List<Entry>
    {
        // ---- Gym (9) ----------------------------------------------------------------------
        new("gym-full-body-a", "Full Body A", "Classic barbell full-body session built on the big three plus a press.",
        [
            new("Barbell Back Squat", Sets: 3, Reps: 5),
            new("Barbell Bench Press", Sets: 3, Reps: 5),
            new("Bent Over Barbell Row", Sets: 3, Reps: 5),
            new("Overhead Press", Sets: 3, Reps: 8),
            new("Plank", Sets: 3, DurationSeconds: 45),
        ]),
        new("gym-full-body-b", "Full Body B", "The alternate full-body day: pull from the floor, then accessories.",
        [
            new("Barbell Deadlift", Sets: 1, Reps: 5),
            new("Incline Barbell Bench Press", Sets: 3, Reps: 8),
            new("Lat Pulldown", Sets: 3, Reps: 10),
            new("Goblet Squat", Sets: 3, Reps: 10),
            new("Hanging Leg Raises", Sets: 3, Reps: 10),
        ]),
        new("gym-push", "Push Day", "Chest, shoulders and triceps.",
        [
            new("Barbell Bench Press", Sets: 4, Reps: 6),
            new("Incline Dumbbell Press", Sets: 3, Reps: 10),
            new("Overhead Press", Sets: 3, Reps: 8),
            new("Lateral Raises", Sets: 3, Reps: 15),
            new("Tricep Pushdown", Sets: 3, Reps: 12),
        ]),
        new("gym-pull", "Pull Day", "Back and biceps, heaviest first.",
        [
            new("Barbell Deadlift", Sets: 3, Reps: 5),
            new("Pull-Ups", Sets: 3, Reps: 8),
            new("Seated Cable Row", Sets: 3, Reps: 10),
            new("Face Pulls", Sets: 3, Reps: 15),
            new("Barbell Curl", Sets: 3, Reps: 10),
        ]),
        new("gym-legs", "Leg Day", "Squat, hinge, and single-leg work.",
        [
            new("Barbell Back Squat", Sets: 4, Reps: 6),
            new("Romanian Deadlift", Sets: 3, Reps: 8),
            new("Leg Press", Sets: 3, Reps: 12),
            new("Walking Lunges", Sets: 3, Reps: 12),
            new("Calf Raises", Sets: 4, Reps: 15),
        ]),
        new("gym-upper", "Upper Body", "A full upper-body day for an upper/lower split.",
        [
            new("Barbell Bench Press", Sets: 4, Reps: 6),
            new("Bent Over Barbell Row", Sets: 4, Reps: 8),
            new("Dumbbell Shoulder Press", Sets: 3, Reps: 10),
            new("Lat Pulldown", Sets: 3, Reps: 10),
            new("Hammer Curl", Sets: 3, Reps: 12),
            new("Skull Crushers", Sets: 3, Reps: 12),
        ]),
        new("gym-lower", "Lower Body", "The lower half of an upper/lower split.",
        [
            new("Barbell Back Squat", Sets: 4, Reps: 6),
            new("Romanian Deadlift", Sets: 3, Reps: 8),
            new("Bulgarian Split Squat", Sets: 3, Reps: 10),
            new("Leg Curl", Sets: 3, Reps: 12),
            new("Calf Raises", Sets: 4, Reps: 15),
        ]),
        new("gym-arms-shoulders", "Arms & Shoulders", "An accessory block for delts and arms.",
        [
            new("Arnold Press", Sets: 3, Reps: 10),
            new("Lateral Raises", Sets: 3, Reps: 15),
            new("Barbell Curl", Sets: 3, Reps: 10),
            new("Tricep Pushdown", Sets: 3, Reps: 12),
            new("Hammer Curl", Sets: 3, Reps: 12),
            new("Shrugs", Sets: 3, Reps: 15),
        ]),
        new("gym-machine-circuit", "Machine Circuit", "Beginner-friendly: every movement is a fixed path.",
        [
            new("Leg Press", Sets: 3, Reps: 12),
            new("Pec Deck Machine", Sets: 3, Reps: 12),
            new("Lat Pulldown", Sets: 3, Reps: 12),
            new("Seated Cable Row", Sets: 3, Reps: 12),
            new("Leg Extension", Sets: 3, Reps: 15),
            new("Leg Curl", Sets: 3, Reps: 15),
        ]),

        // ---- Home (6) ---------------------------------------------------------------------
        new("home-bodyweight-full-body", "Bodyweight Full Body", "No equipment beyond something to pull on.",
        [
            new("Push-Ups", Sets: 3, Reps: 12),
            new("Inverted Row", Sets: 3, Reps: 10),
            new("Bodyweight Squat", Sets: 3, Reps: 20),
            new("Walking Lunges", Sets: 3, Reps: 12),
            new("Plank", Sets: 3, DurationSeconds: 45),
            new("Mountain Climbers", Sets: 3, DurationSeconds: 30),
        ]),
        new("home-core-express", "Core Express", "Fifteen minutes, floor only.",
        [
            new("Plank", Sets: 3, DurationSeconds: 45),
            new("Side Plank", Sets: 2, DurationSeconds: 30, Notes: "Each side."),
            new("Crunches", Sets: 3, Reps: 20),
            new("Russian Twists", Sets: 3, Reps: 20),
            new("Mountain Climbers", Sets: 3, DurationSeconds: 30),
        ]),
        new("home-dumbbell-full-body", "Dumbbell Full Body", "One pair of dumbbells, whole body.",
        [
            new("Dumbbell Bench Press", Sets: 3, Reps: 10),
            new("Dumbbell Row", Sets: 3, Reps: 10, Notes: "Each side."),
            new("Dumbbell Shoulder Press", Sets: 3, Reps: 10),
            new("Bulgarian Split Squat", Sets: 3, Reps: 10, Notes: "Each side."),
            new("Hammer Curl", Sets: 3, Reps: 12),
        ]),
        new("home-dumbbell-upper", "Dumbbell Upper Body", "Upper body from a single pair of dumbbells.",
        [
            new("Dumbbell Bench Press", Sets: 3, Reps: 10),
            new("Dumbbell Row", Sets: 3, Reps: 10, Notes: "Each side."),
            new("Arnold Press", Sets: 3, Reps: 10),
            new("Dumbbell Curl", Sets: 3, Reps: 12),
            new("Overhead Tricep Extension", Sets: 3, Reps: 12),
        ]),
        new("home-rack-full-body", "Squat Rack Full Body", "A rack, a bar and a pull-up bar.",
        [
            new("Barbell Back Squat", Sets: 3, Reps: 5),
            new("Barbell Bench Press", Sets: 3, Reps: 5),
            new("Bent Over Barbell Row", Sets: 3, Reps: 8),
            new("Overhead Press", Sets: 3, Reps: 8),
            new("Chin-Ups", Sets: 3, Reps: 8),
        ]),
        new("home-rack-lower", "Squat Rack Lower Body", "Two squat patterns and a hinge.",
        [
            new("Barbell Back Squat", Sets: 4, Reps: 6),
            new("Romanian Deadlift", Sets: 3, Reps: 8),
            new("Front Squat", Sets: 3, Reps: 8),
            new("Walking Lunges", Sets: 3, Reps: 12),
            new("Calf Raises", Sets: 4, Reps: 15),
        ]),

        // ---- Outdoor (5) ------------------------------------------------------------------
        new("outdoor-easy-run", "Easy Run", "Conversational pace — you should be able to talk throughout.",
        [
            new("Running", DurationSeconds: 1800, DistanceKm: 5m, Notes: "Conversational pace."),
        ]),
        new("outdoor-run-intervals", "Run Intervals", "Short, fast repeats with a walk either side.",
        [
            new("Walking", DurationSeconds: 300, Notes: "Warm-up."),
            new("Sprint Intervals", Sets: 8, DistanceKm: 0.4m, Notes: "400 m hard, walk back to recover."),
            new("Running", DurationSeconds: 300, Notes: "Cool-down."),
        ]),
        new("outdoor-park-calisthenics", "Park Calisthenics", "A bar, a dip station and the ground.",
        [
            new("Pull-Ups", Sets: 4, Reps: 8),
            new("Chest Dips", Sets: 3, Reps: 10),
            new("Push-Ups", Sets: 3, Reps: 15),
            new("Inverted Row", Sets: 3, Reps: 12),
            new("Hanging Leg Raises", Sets: 3, Reps: 10),
        ]),
        new("outdoor-conditioning-circuit", "Outdoor Conditioning Circuit", "Move continuously; rest only between rounds.",
        [
            new("Burpees", Sets: 4, Reps: 10),
            new("Box Jumps", Sets: 4, Reps: 10),
            new("Mountain Climbers", Sets: 4, DurationSeconds: 30),
            new("Jump Rope", Sets: 4, DurationSeconds: 60),
            new("Bodyweight Squat", Sets: 4, Reps: 20),
        ]),
        new("outdoor-long-ride", "Long Ride", "Steady aerobic work on the bike.",
        [
            new("Cycling", DurationSeconds: 3600, DistanceKm: 20m, Notes: "Steady effort."),
        ]),

        // ---- Warm-ups (5) -----------------------------------------------------------------
        new("warmup-general-dynamic", "General Dynamic Warm-Up", "Raises the heart rate and opens the main joints.",
        [
            new("Jumping Jacks", DurationSeconds: 60),
            new("Arm Circles", DurationSeconds: 30, Notes: "Fifteen seconds each direction."),
            new("Leg Swings", Sets: 1, Reps: 10, Notes: "Each side, front to back then side to side."),
            new("World's Greatest Stretch", Sets: 1, Reps: 5, Notes: "Each side."),
            new("High Knees", DurationSeconds: 30),
        ]),
        new("warmup-upper-push", "Upper Body Warm-Up (Push)", "Shoulders and scapulae before pressing.",
        [
            new("Arm Circles", DurationSeconds: 30, Notes: "Fifteen seconds each direction."),
            new("Band Pull-Aparts", Sets: 2, Reps: 15),
            new("Scapular Push-Ups", Sets: 2, Reps: 10),
            new("Wall Slides", Sets: 2, Reps: 10),
            new("Push-Ups", Sets: 1, Reps: 10, Notes: "Light — this is a rehearsal, not a set."),
        ]),
        new("warmup-lower-squat-hinge", "Lower Body Warm-Up (Squat/Hinge)", "Hips and glutes before squatting or pulling.",
        [
            new("Bodyweight Squat", Sets: 2, Reps: 10),
            new("Hip Circles", DurationSeconds: 30, Notes: "Each direction."),
            new("Leg Swings", Sets: 1, Reps: 10, Notes: "Each side."),
            new("Glute Bridge", Sets: 2, Reps: 12),
            new("Walking Lunges", Sets: 1, Reps: 10),
        ]),
        new("warmup-pull", "Pull Warm-Up (Back/Biceps)", "Upper back and lats before pulling.",
        [
            new("Band Pull-Aparts", Sets: 2, Reps: 15),
            new("Scapular Pull-Ups", Sets: 2, Reps: 8),
            new("Cat-Cow", DurationSeconds: 60),
            new("Dead Hang", Sets: 2, DurationSeconds: 20),
            new("Face Pulls", Sets: 1, Reps: 15, Notes: "Light."),
        ]),
        new("warmup-run", "Run Warm-Up", "Walk in, then open the stride before running.",
        [
            new("Walking", DurationSeconds: 300, Notes: "Brisk."),
            new("Leg Swings", Sets: 1, Reps: 10, Notes: "Each side."),
            new("High Knees", DurationSeconds: 30),
            new("Butt Kicks", DurationSeconds: 30),
            new("Ankle Circles", DurationSeconds: 30, Notes: "Each direction."),
        ]),
    };
}
