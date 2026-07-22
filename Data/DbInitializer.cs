using FitTracker.Models;
using FitTracker.Services;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var changesMade = false;

        if (!await context.Exercises.AnyAsync())
        {
            var exercises = new List<Exercise>
            {
                // Chest Exercises
                new() { Name = "Barbell Bench Press", Category = "Strength", MuscleGroups = "Chest,Triceps,Shoulders", Equipment = "Barbell", Description = "Classic compound chest exercise", VideoUrl = "https://www.youtube.com/watch?v=rT7DgCr-3pg" },
                new() { Name = "Dumbbell Bench Press", Category = "Strength", MuscleGroups = "Chest,Triceps,Shoulders", Equipment = "Dumbbells", Description = "Dumbbell variation of bench press" },
                new() { Name = "Incline Barbell Bench Press", Category = "Strength", MuscleGroups = "Chest,Triceps,Shoulders", Equipment = "Barbell", Description = "Targets upper chest" },
                new() { Name = "Incline Dumbbell Press", Category = "Strength", MuscleGroups = "Chest,Triceps,Shoulders", Equipment = "Dumbbells", Description = "Upper chest focus with dumbbells" },
                new() { Name = "Decline Bench Press", Category = "Strength", MuscleGroups = "Chest,Triceps", Equipment = "Barbell", Description = "Lower chest emphasis" },
                new() { Name = "Push-Ups", Category = "Strength", MuscleGroups = "Chest,Triceps,Shoulders,Core", Equipment = "Bodyweight", Description = "Bodyweight chest exercise" },
                new() { Name = "Dumbbell Flyes", Category = "Strength", MuscleGroups = "Chest", Equipment = "Dumbbells", Description = "Isolation exercise for chest" },
                new() { Name = "Cable Crossover", Category = "Strength", MuscleGroups = "Chest", Equipment = "Cable", Description = "Cable chest isolation" },
                new() { Name = "Chest Dips", Category = "Strength", MuscleGroups = "Chest,Triceps,Shoulders", Equipment = "Bodyweight", Description = "Bodyweight compound for chest" },
                new() { Name = "Pec Deck Machine", Category = "Strength", MuscleGroups = "Chest", Equipment = "Machine", Description = "Machine chest isolation" },

                // Back Exercises
                new() { Name = "Barbell Deadlift", Category = "Strength", MuscleGroups = "Back,Legs,Core", Equipment = "Barbell", Description = "King of compound movements", VideoUrl = "https://www.youtube.com/watch?v=XxWcirHIwVo" },
                new() { Name = "Pull-Ups", Category = "Strength", MuscleGroups = "Back,Biceps", Equipment = "Bodyweight", Description = "Bodyweight back builder" },
                new() { Name = "Bent Over Barbell Row", Category = "Strength", MuscleGroups = "Back,Biceps", Equipment = "Barbell", Description = "Classic back exercise" },
                new() { Name = "Dumbbell Row", Category = "Strength", MuscleGroups = "Back,Biceps", Equipment = "Dumbbells", Description = "Unilateral back work" },
                new() { Name = "Lat Pulldown", Category = "Strength", MuscleGroups = "Back,Biceps", Equipment = "Cable", Description = "Cable lat exercise" },
                new() { Name = "Seated Cable Row", Category = "Strength", MuscleGroups = "Back,Biceps", Equipment = "Cable", Description = "Seated rowing motion" },
                new() { Name = "T-Bar Row", Category = "Strength", MuscleGroups = "Back,Biceps", Equipment = "Barbell", Description = "Thick back builder" },
                new() { Name = "Face Pulls", Category = "Strength", MuscleGroups = "Back,Shoulders", Equipment = "Cable", Description = "Rear delt and upper back" },
                new() { Name = "Chin-Ups", Category = "Strength", MuscleGroups = "Back,Biceps", Equipment = "Bodyweight", Description = "Underhand pull-ups" },
                new() { Name = "Inverted Row", Category = "Strength", MuscleGroups = "Back,Biceps", Equipment = "Bodyweight", Description = "Horizontal pulling" },

                // Leg Exercises
                new() { Name = "Barbell Back Squat", Category = "Strength", MuscleGroups = "Legs,Glutes,Core", Equipment = "Barbell", Description = "King of leg exercises", VideoUrl = "https://www.youtube.com/watch?v=ultWZbUMPL8" },
                new() { Name = "Front Squat", Category = "Strength", MuscleGroups = "Legs,Core", Equipment = "Barbell", Description = "Quad-focused squat" },
                new() { Name = "Romanian Deadlift", Category = "Strength", MuscleGroups = "Hamstrings,Glutes,Back", Equipment = "Barbell", Description = "Hamstring focused" },
                new() { Name = "Leg Press", Category = "Strength", MuscleGroups = "Legs,Glutes", Equipment = "Machine", Description = "Machine leg builder" },
                new() { Name = "Bulgarian Split Squat", Category = "Strength", MuscleGroups = "Legs,Glutes", Equipment = "Dumbbells", Description = "Unilateral leg work" },
                new() { Name = "Leg Extension", Category = "Strength", MuscleGroups = "Quadriceps", Equipment = "Machine", Description = "Quad isolation" },
                new() { Name = "Leg Curl", Category = "Strength", MuscleGroups = "Hamstrings", Equipment = "Machine", Description = "Hamstring isolation" },
                new() { Name = "Walking Lunges", Category = "Strength", MuscleGroups = "Legs,Glutes", Equipment = "Dumbbells", Description = "Dynamic leg exercise" },
                new() { Name = "Calf Raises", Category = "Strength", MuscleGroups = "Calves", Equipment = "Bodyweight", Description = "Calf development" },
                new() { Name = "Goblet Squat", Category = "Strength", MuscleGroups = "Legs,Core", Equipment = "Dumbbell", Description = "Beginner-friendly squat" },

                // Shoulder Exercises
                new() { Name = "Overhead Press", Category = "Strength", MuscleGroups = "Shoulders,Triceps", Equipment = "Barbell", Description = "Primary shoulder builder", VideoUrl = "https://www.youtube.com/watch?v=2yjwXTZQDDI" },
                new() { Name = "Dumbbell Shoulder Press", Category = "Strength", MuscleGroups = "Shoulders,Triceps", Equipment = "Dumbbells", Description = "Dumbbell pressing" },
                new() { Name = "Lateral Raises", Category = "Strength", MuscleGroups = "Shoulders", Equipment = "Dumbbells", Description = "Side delt isolation" },
                new() { Name = "Front Raises", Category = "Strength", MuscleGroups = "Shoulders", Equipment = "Dumbbells", Description = "Front delt isolation" },
                new() { Name = "Rear Delt Flyes", Category = "Strength", MuscleGroups = "Shoulders", Equipment = "Dumbbells", Description = "Rear delt work" },
                new() { Name = "Arnold Press", Category = "Strength", MuscleGroups = "Shoulders", Equipment = "Dumbbells", Description = "All-around shoulder work" },
                new() { Name = "Upright Row", Category = "Strength", MuscleGroups = "Shoulders,Traps", Equipment = "Barbell", Description = "Shoulder and trap builder" },
                new() { Name = "Shrugs", Category = "Strength", MuscleGroups = "Traps", Equipment = "Dumbbells", Description = "Trap isolation" },

                // Arm Exercises
                new() { Name = "Barbell Curl", Category = "Strength", MuscleGroups = "Biceps", Equipment = "Barbell", Description = "Classic bicep builder" },
                new() { Name = "Dumbbell Curl", Category = "Strength", MuscleGroups = "Biceps", Equipment = "Dumbbells", Description = "Dumbbell bicep work" },
                new() { Name = "Hammer Curl", Category = "Strength", MuscleGroups = "Biceps,Forearms", Equipment = "Dumbbells", Description = "Neutral grip curls" },
                new() { Name = "Tricep Pushdown", Category = "Strength", MuscleGroups = "Triceps", Equipment = "Cable", Description = "Cable tricep isolation" },
                new() { Name = "Skull Crushers", Category = "Strength", MuscleGroups = "Triceps", Equipment = "Barbell", Description = "Lying tricep extension" },
                new() { Name = "Close-Grip Bench Press", Category = "Strength", MuscleGroups = "Triceps,Chest", Equipment = "Barbell", Description = "Compound tricep builder" },
                new() { Name = "Overhead Tricep Extension", Category = "Strength", MuscleGroups = "Triceps", Equipment = "Dumbbell", Description = "Overhead tricep work" },
                new() { Name = "Concentration Curl", Category = "Strength", MuscleGroups = "Biceps", Equipment = "Dumbbell", Description = "Isolated bicep curl" },
                new() { Name = "Preacher Curl", Category = "Strength", MuscleGroups = "Biceps", Equipment = "Barbell", Description = "Strict bicep curl" },

                // Core Exercises
                new() { Name = "Plank", Category = "Core", MuscleGroups = "Core", Equipment = "Bodyweight", Description = "Isometric core hold" },
                new() { Name = "Crunches", Category = "Core", MuscleGroups = "Abs", Equipment = "Bodyweight", Description = "Basic ab exercise" },
                new() { Name = "Hanging Leg Raises", Category = "Core", MuscleGroups = "Abs", Equipment = "Bodyweight", Description = "Advanced ab work" },
                new() { Name = "Russian Twists", Category = "Core", MuscleGroups = "Abs,Obliques", Equipment = "Bodyweight", Description = "Rotational core work" },
                new() { Name = "Ab Wheel Rollout", Category = "Core", MuscleGroups = "Core", Equipment = "Ab Wheel", Description = "Advanced core stability" },
                new() { Name = "Mountain Climbers", Category = "Core", MuscleGroups = "Core,Cardio", Equipment = "Bodyweight", Description = "Dynamic core exercise" },
                new() { Name = "Side Plank", Category = "Core", MuscleGroups = "Obliques,Core", Equipment = "Bodyweight", Description = "Lateral core strength" },
                new() { Name = "Cable Woodchop", Category = "Core", MuscleGroups = "Obliques,Core", Equipment = "Cable", Description = "Rotational cable work" },

                // Cardio Exercises
                new() { Name = "Running", Category = "Cardio", MuscleGroups = "Legs,Cardio", Equipment = "None", Description = "Classic cardio" },
                new() { Name = "Cycling", Category = "Cardio", MuscleGroups = "Legs,Cardio", Equipment = "Bike", Description = "Low-impact cardio" },
                new() { Name = "Rowing Machine", Category = "Cardio", MuscleGroups = "Full Body,Cardio", Equipment = "Machine", Description = "Full body cardio" },
                new() { Name = "Jump Rope", Category = "Cardio", MuscleGroups = "Legs,Cardio", Equipment = "Jump Rope", Description = "High-intensity cardio" },
                new() { Name = "Burpees", Category = "Cardio", MuscleGroups = "Full Body,Cardio", Equipment = "Bodyweight", Description = "Full body conditioning" },
                new() { Name = "Box Jumps", Category = "Cardio", MuscleGroups = "Legs,Power", Equipment = "Box", Description = "Explosive leg power" },
                new() { Name = "Battle Ropes", Category = "Cardio", MuscleGroups = "Arms,Cardio", Equipment = "Battle Ropes", Description = "Upper body cardio" },
                new() { Name = "Stair Climber", Category = "Cardio", MuscleGroups = "Legs,Cardio", Equipment = "Machine", Description = "Climbing cardio" },
            };

            foreach (var exercise in exercises)
            {
                exercise.TracksOneRepMax = IsWeightLoaded(exercise.Category, exercise.Equipment);
            }

            await context.Exercises.AddRangeAsync(exercises);
            changesMade = true;
        }

        // Seeded per name rather than behind the whole-table guard above, because these arrived
        // after the library did: on any database that already has exercises the guard would skip
        // them forever, and the catalog below resolves exercises by name and fails startup if one
        // is missing (WDM-43, WDM-44).
        var additionalExercises = new List<Exercise>
        {
            new() { Name = "Bodyweight Squat", Category = "Strength", MuscleGroups = "Legs,Glutes", Equipment = "Bodyweight", Description = "Unloaded squat pattern" },
            new() { Name = "Glute Bridge", Category = "Strength", MuscleGroups = "Glutes,Hamstrings", Equipment = "Bodyweight", Description = "Glute activation from the floor" },
            new() { Name = "Walking", Category = "Cardio", MuscleGroups = "Legs,Cardio", Equipment = "None", Description = "Easy-paced walking" },
            new() { Name = "Sprint Intervals", Category = "Cardio", MuscleGroups = "Legs,Cardio", Equipment = "None", Description = "Short maximal efforts with recovery" },
            new() { Name = "Jumping Jacks", Category = "Cardio", MuscleGroups = "Full Body,Cardio", Equipment = "Bodyweight", Description = "Whole-body warm-up movement" },
            new() { Name = "High Knees", Category = "Cardio", MuscleGroups = "Legs,Cardio", Equipment = "Bodyweight", Description = "Running drill on the spot" },
            new() { Name = "Butt Kicks", Category = "Cardio", MuscleGroups = "Hamstrings,Cardio", Equipment = "Bodyweight", Description = "Running drill for the hamstrings" },

            // Mobility is a new category, added for warm-up and activation work. The seed-time
            // IsWeightLoaded rule leaves TracksOneRepMax false for every entry in it, which is
            // correct: none of these has a meaningful one-rep max (D7, WDM-44).
            new() { Name = "Arm Circles", Category = "Mobility", MuscleGroups = "Shoulders", Equipment = "Bodyweight", Description = "Shoulder warm-up" },
            new() { Name = "Leg Swings", Category = "Mobility", MuscleGroups = "Legs,Hips", Equipment = "Bodyweight", Description = "Dynamic hip mobility" },
            new() { Name = "Hip Circles", Category = "Mobility", MuscleGroups = "Hips", Equipment = "Bodyweight", Description = "Hip joint mobility" },
            new() { Name = "Ankle Circles", Category = "Mobility", MuscleGroups = "Calves", Equipment = "Bodyweight", Description = "Ankle mobility" },
            new() { Name = "World's Greatest Stretch", Category = "Mobility", MuscleGroups = "Hips,Back,Shoulders", Equipment = "Bodyweight", Description = "Full-body dynamic stretch" },
            new() { Name = "Cat-Cow", Category = "Mobility", MuscleGroups = "Back,Core", Equipment = "Bodyweight", Description = "Spinal flexion and extension" },
            new() { Name = "Scapular Push-Ups", Category = "Mobility", MuscleGroups = "Shoulders,Chest", Equipment = "Bodyweight", Description = "Scapular protraction and retraction" },
            new() { Name = "Scapular Pull-Ups", Category = "Mobility", MuscleGroups = "Back,Shoulders", Equipment = "Bodyweight", Description = "Scapular control from a hang" },
            new() { Name = "Wall Slides", Category = "Mobility", MuscleGroups = "Shoulders,Back", Equipment = "Bodyweight", Description = "Overhead shoulder mobility" },
            new() { Name = "Dead Hang", Category = "Mobility", MuscleGroups = "Back,Forearms", Equipment = "Bodyweight", Description = "Passive hang for shoulder decompression" },
            new() { Name = "Band Pull-Aparts", Category = "Mobility", MuscleGroups = "Back,Shoulders", Equipment = "Resistance Band", Description = "Upper back activation" },
        };

        var existingExerciseNames = await context.Exercises
            .Select(exercise => exercise.Name)
            .ToListAsync();

        var missingExercises = additionalExercises
            .Where(exercise => !existingExerciseNames.Contains(exercise.Name))
            .ToList();

        if (missingExercises.Count > 0)
        {
            foreach (var exercise in missingExercises)
            {
                exercise.TracksOneRepMax = IsWeightLoaded(exercise.Category, exercise.Equipment);
            }

            await context.Exercises.AddRangeAsync(missingExercises);
            changesMade = true;
        }

        if (!await context.Achievements.AnyAsync())
        {
            await context.Achievements.AddRangeAsync(
                new Achievement { Name = "First Workout", Description = "Complete your first workout.", Icon = "🏁", Criteria = $"{AchievementCriteria.CompletedWorkouts}:1" },
                new Achievement { Name = "10 Workouts", Description = "Finish 10 completed workouts.", Icon = "🔥", Criteria = $"{AchievementCriteria.CompletedWorkouts}:10" },
                new Achievement { Name = "30-Day Streak", Description = "Train 30 days in a row.", Icon = "📆", Criteria = $"{AchievementCriteria.CurrentStreak}:30" },
                new Achievement { Name = "100 Total Sets", Description = "Log 100 total sets across completed workouts.", Icon = "💯", Criteria = $"{AchievementCriteria.TotalSets}:100" },
                new Achievement { Name = "First PR", Description = "Unlock your first personal record.", Icon = "🏆", Criteria = $"{AchievementCriteria.PersonalRecords}:1" },
                new Achievement { Name = "10 PRs", Description = "Reach 10 personal records.", Icon = "⚡", Criteria = $"{AchievementCriteria.PersonalRecords}:10" },
                new Achievement { Name = "1M Volume", Description = "Accumulate 1,000,000 total volume.", Icon = "🚀", Criteria = $"{AchievementCriteria.TotalVolume}:1000000" });
            changesMade = true;
        }

        if (!await context.Challenges.AnyAsync())
        {
            // No dates here on purpose: the window starts when a user joins, so
            // these stay valid however long after seeding they are picked up.
            await context.Challenges.AddRangeAsync(
                new Challenge
                {
                    Name = "30-Day Workout Challenge",
                    Description = "Complete 20 workouts within 30 days of joining.",
                    Icon = "📅",
                    GoalType = ChallengeGoalTypes.CompletedWorkouts,
                    Goal = 20m,
                    DurationDays = 30
                },
                new Challenge
                {
                    Name = "Consistency Sprint",
                    Description = "Complete 6 workouts in a single week.",
                    Icon = "⚡",
                    GoalType = ChallengeGoalTypes.CompletedWorkouts,
                    Goal = 6m,
                    DurationDays = 7
                },
                new Challenge
                {
                    Name = "Volume Challenge",
                    Description = "Move 100,000 of total volume within 30 days.",
                    Icon = "🏋️",
                    GoalType = ChallengeGoalTypes.TotalVolume,
                    Goal = 100000m,
                    DurationDays = 30
                },
                new Challenge
                {
                    Name = "Set Grinder",
                    Description = "Log 200 sets within 30 days.",
                    Icon = "🔁",
                    GoalType = ChallengeGoalTypes.TotalSets,
                    Goal = 200m,
                    DurationDays = 30
                });
            changesMade = true;
        }

        if (changesMade)
        {
            await context.SaveChangesAsync();
        }

        // Seeded after the SaveChanges above, because it resolves exercises by name and the
        // additions made in this run have to be queryable first.
        await SeedTemplateCatalogAsync(context);
    }

    /// <summary>
    /// Inserts every catalog entry that is absent by <see cref="WorkoutTemplate.CatalogKey"/>, and
    /// leaves the ones already present exactly as they are (WDM-42, D6). Unlike the whole-table
    /// guards above, this runs per entry, so a template added to the catalog in a later release
    /// reaches a database that was seeded by an earlier one — and re-running it is a no-op.
    /// </summary>
    private static async Task SeedTemplateCatalogAsync(ApplicationDbContext context)
    {
        var seededKeys = await context.WorkoutTemplates
            .Where(template => template.CatalogKey != null)
            .Select(template => template.CatalogKey!)
            .ToListAsync();

        var missingEntries = TemplateCatalog.Entries
            .Where(entry => !seededKeys.Contains(entry.CatalogKey))
            .ToList();

        if (missingEntries.Count == 0)
            return;

        var exerciseIdsByName = await context.Exercises
            .Select(exercise => new { exercise.Name, exercise.Id })
            .ToDictionaryAsync(exercise => exercise.Name, exercise => exercise.Id);

        foreach (var entry in missingEntries)
        {
            var template = new WorkoutTemplate
            {
                // Ownerless: a built-in belongs to nobody and is visible to everybody (WDM-04).
                UserId = null,
                CatalogKey = entry.CatalogKey,
                Name = entry.Name,
                Description = entry.Description,
                IsActive = true
            };

            template.Exercises = entry.Exercises
                .Select((catalogExercise, index) => new WorkoutTemplateExercise
                {
                    // Fail the whole startup rather than seed a template with a hole in it. A
                    // renamed or missing exercise is a bug in the catalog data, and it should be
                    // impossible to miss (WDM-43, WDM-NF-03).
                    ExerciseId = exerciseIdsByName.TryGetValue(catalogExercise.ExerciseName, out var exerciseId)
                        ? exerciseId
                        : throw new InvalidOperationException(
                            $"Template catalog entry '{entry.CatalogKey}' references the exercise " +
                            $"'{catalogExercise.ExerciseName}', which is not in the seeded library."),
                    Order = index + 1,
                    DefaultSets = catalogExercise.Sets,
                    DefaultReps = catalogExercise.Reps,
                    DefaultDurationSeconds = catalogExercise.DurationSeconds,
                    DefaultDistance = catalogExercise.DistanceKm,
                    Notes = catalogExercise.Notes
                })
                .ToList();

            context.WorkoutTemplates.Add(template);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed-time default for <see cref="Exercise.TracksOneRepMax"/>: strength work carries an
    /// external load unless the equipment is the lifter. Cardio and core holds never do. The
    /// AddOneRepMaxTracking migration backfills existing databases with the same rule; both are
    /// starting points, and the flag is per exercise so it can be corrected afterwards.
    /// </summary>
    private static bool IsWeightLoaded(string category, string equipment) =>
        string.Equals(category, "Strength", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(equipment, "Bodyweight", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(equipment, "None", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(equipment);
}
