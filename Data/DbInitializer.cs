using FitTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if exercises already exist
        if (await context.Exercises.AnyAsync())
        {
            return; // Database has been seeded
        }

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

        await context.Exercises.AddRangeAsync(exercises);
        await context.SaveChangesAsync();
    }
}
