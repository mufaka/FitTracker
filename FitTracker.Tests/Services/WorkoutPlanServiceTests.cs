using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitTracker.Tests.Services;

public class WorkoutPlanServiceTests
{
    [Fact]
    public async Task GetPlansAsync_ExcludesOtherUsersPlansAndSoftDeletedOnes()
    {
        // WDM-TEST-01.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var service = new WorkoutPlanService(context);

        await service.SavePlanAsync(fixture.Mine, NewPlan("Mine — active", fixture.SquatId));
        var retiredId = await service.SavePlanAsync(fixture.Mine, NewPlan("Mine — retired", fixture.SquatId));
        var deletedId = await service.SavePlanAsync(fixture.Mine, NewPlan("Mine — deleted", fixture.SquatId));
        await service.SavePlanAsync(fixture.Theirs, NewPlan("Theirs", fixture.SquatId));

        Assert.True(await service.SetPlanActiveAsync(retiredId!.Value, fixture.Mine, false));
        Assert.True(await service.DeletePlanAsync(deletedId!.Value, fixture.Mine));

        var plans = await service.GetPlansAsync(fixture.Mine);

        // Retired stays listed so it can be brought back; deleted disappears; theirs was never visible.
        Assert.Equal(new[] { "Mine — active", "Mine — retired" }, plans.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task GetActivePlansAsync_ReturnsOnlyPlansThatCouldGuideAWorkout()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var service = new WorkoutPlanService(context);

        await service.SavePlanAsync(fixture.Mine, NewPlan("Active", fixture.SquatId));
        var retiredId = await service.SavePlanAsync(fixture.Mine, NewPlan("Retired", fixture.SquatId));
        await service.SetPlanActiveAsync(retiredId!.Value, fixture.Mine, false);

        var active = await service.GetActivePlansAsync(fixture.Mine);

        Assert.Equal("Active", Assert.Single(active).Name);
    }

    [Fact]
    public async Task DeletePlanAsync_SoftDeletesSoTheRowSurvivesForAnyWorkoutPointingAtIt()
    {
        // WDM-TEST-04.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var service = new WorkoutPlanService(context);

        var planId = await service.SavePlanAsync(fixture.Mine, NewPlan("Leg day", fixture.SquatId));

        context.Workouts.Add(new Workout
        {
            UserId = fixture.Mine,
            Date = DateTime.UtcNow,
            Duration = 45,
            IsCompleted = true,
            WorkoutPlanId = planId!.Value,
            WorkoutExercises =
            {
                new WorkoutExercise
                {
                    ExerciseId = fixture.SquatId,
                    Order = 1,
                    Sets = { new Set { SetNumber = 1, Weight = 100m, Reps = 5 } }
                }
            }
        });
        await context.SaveChangesAsync();

        Assert.True(await service.DeletePlanAsync(planId.Value, fixture.Mine));

        Assert.Empty(await service.GetPlansAsync(fixture.Mine));

        // Still in the database, so the workout's reference still resolves and its sets are intact.
        var stored = await context.WorkoutPlans.AsNoTracking().SingleAsync(p => p.Id == planId.Value);
        Assert.True(stored.IsDeleted);
        Assert.False(stored.IsActive);

        var workout = await context.Workouts.AsNoTracking()
            .Include(w => w.WorkoutExercises).ThenInclude(we => we.Sets)
            .SingleAsync(w => w.WorkoutPlanId == planId.Value);
        Assert.Equal(100m, Assert.Single(Assert.Single(workout.WorkoutExercises).Sets).Weight);
    }

    [Fact]
    public async Task SaveAndDelete_AreRejectedForAPlanTheCallerDoesNotOwn()
    {
        // WDM-TEST-03.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var service = new WorkoutPlanService(context);

        var theirPlanId = await service.SavePlanAsync(fixture.Theirs, NewPlan("Theirs", fixture.SquatId));

        var hijack = NewPlan("Hijacked", fixture.SquatId);
        hijack.Id = theirPlanId;

        Assert.Null(await service.SavePlanAsync(fixture.Mine, hijack));
        Assert.False(await service.DeletePlanAsync(theirPlanId!.Value, fixture.Mine));
        Assert.False(await service.SetPlanActiveAsync(theirPlanId.Value, fixture.Mine, false));
        Assert.Null(await service.GetPlanEditorAsync(theirPlanId.Value, fixture.Mine));

        var stored = await context.WorkoutPlans.AsNoTracking().SingleAsync(p => p.Id == theirPlanId.Value);
        Assert.Equal("Theirs", stored.Name);
        Assert.False(stored.IsDeleted);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task ApplyTemplateAsync_AppendsInTemplateOrderWithItsPrescriptionAndKeepsDuplicates()
    {
        // WDM-TEST-02. Applying a template must not merge, deduplicate or drop anything: a warm-up's
        // light push-ups and a main block's working push-ups are different entries.
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);

        context.WorkoutTemplates.Add(new WorkoutTemplate
        {
            UserId = null,
            CatalogKey = "warmup-lower",
            Name = "Lower warm-up",
            Exercises =
            {
                new WorkoutTemplateExercise { ExerciseId = fixture.SquatId, Order = 1, DefaultSets = 2, DefaultReps = 10 },
                new WorkoutTemplateExercise { ExerciseId = fixture.RunId, Order = 2, DefaultDurationSeconds = 300, DefaultDistance = 5m }
            }
        });
        await context.SaveChangesAsync();
        var templateId = await context.WorkoutTemplates.Where(t => t.CatalogKey == "warmup-lower").Select(t => t.Id).SingleAsync();

        var service = new WorkoutPlanService(context);

        // The plan already contains the squat, at a working prescription.
        var model = NewPlan("Leg day", fixture.SquatId);
        model.Exercises[0].TargetSets = 5;
        model.Exercises[0].TargetReps = 5;

        var applied = await service.ApplyTemplateAsync(templateId, fixture.Mine, model);

        Assert.NotNull(applied);
        Assert.Equal(3, applied!.Exercises.Count);

        // Appended in template order, after what was already there.
        Assert.Equal(new[] { fixture.SquatId, fixture.SquatId, fixture.RunId }, applied.Exercises.Select(e => e.ExerciseId).ToArray());

        // The original entry keeps its own prescription — nothing was merged over it.
        Assert.Equal(5, applied.Exercises[0].TargetSets);
        Assert.Equal(5, applied.Exercises[0].TargetReps);

        // The contributed ones carry the template's prescription.
        Assert.Equal(2, applied.Exercises[1].TargetSets);
        Assert.Equal(10, applied.Exercises[1].TargetReps);
        Assert.Equal(300, applied.Exercises[2].TargetDurationSeconds);
        // 5 km stored, shown to an lbs user in miles.
        Assert.Equal(3.11m, applied.Exercises[2].TargetDistance);
    }

    [Fact]
    public async Task ApplyTemplateAsync_RefusesATemplateTheCallerCannotSee()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);

        context.WorkoutTemplates.Add(new WorkoutTemplate
        {
            UserId = fixture.Theirs,
            Name = "Theirs",
            Exercises = { new WorkoutTemplateExercise { ExerciseId = fixture.SquatId, Order = 1 } }
        });
        await context.SaveChangesAsync();
        var theirTemplateId = await context.WorkoutTemplates.Where(t => t.UserId == fixture.Theirs).Select(t => t.Id).SingleAsync();

        var service = new WorkoutPlanService(context);

        Assert.Null(await service.ApplyTemplateAsync(theirTemplateId, fixture.Mine, NewPlan("Mine", fixture.SquatId)));
    }

    [Fact]
    public async Task IsPlanReferencedAsync_TurnsTrueOnceAWorkoutHasBeenPerformedFromThePlan()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var service = new WorkoutPlanService(context);

        var planId = await service.SavePlanAsync(fixture.Mine, NewPlan("Leg day", fixture.SquatId));

        Assert.False(await service.IsPlanReferencedAsync(planId!.Value, fixture.Mine));

        context.Workouts.Add(new Workout
        {
            UserId = fixture.Mine,
            Date = DateTime.UtcNow,
            Duration = 30,
            WorkoutPlanId = planId.Value
        });
        await context.SaveChangesAsync();

        Assert.True(await service.IsPlanReferencedAsync(planId.Value, fixture.Mine));

        // Whether somebody else has used it is not this caller's business.
        Assert.False(await service.IsPlanReferencedAsync(planId.Value, fixture.Theirs));
    }

    [Fact]
    public async Task SavePlanAsync_RejectsAnExerciseThatDoesNotExist()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var service = new WorkoutPlanService(context);

        Assert.Null(await service.SavePlanAsync(fixture.Mine, NewPlan("Bogus", exerciseId: 9999)));
    }

    [Fact]
    public async Task SavePlanAsync_RenumbersOrderFromListPositionAndStoresDistanceCanonically()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var fixture = await SeedAsync(context);
        var service = new WorkoutPlanService(context);

        var model = new PlanEditorModel
        {
            Name = "Mixed",
            Exercises =
            {
                new PlanExerciseEditorModel { ExerciseId = fixture.RunId, TargetDistance = 3.11m },
                new PlanExerciseEditorModel { ExerciseId = fixture.SquatId, TargetSets = 3, TargetReps = 10 }
            }
        };

        var planId = await service.SavePlanAsync(fixture.Mine, model);
        Assert.NotNull(planId);

        var saved = await context.WorkoutPlanExercises.AsNoTracking()
            .Where(pe => pe.WorkoutPlanId == planId!.Value)
            .OrderBy(pe => pe.Order)
            .ToListAsync();

        Assert.Equal(new[] { 1, 2 }, saved.Select(pe => pe.Order).ToArray());
        Assert.Equal(fixture.RunId, saved[0].ExerciseId);
        Assert.Equal(5.0051m, saved[0].TargetDistance);
        Assert.Null(saved[0].TargetSets);
        Assert.Equal(3, saved[1].TargetSets);
    }

    private static PlanEditorModel NewPlan(string name, int exerciseId) => new()
    {
        Name = name,
        Exercises = { new PlanExerciseEditorModel { ExerciseId = exerciseId } }
    };

    private static async Task<Fixture> SeedAsync(ApplicationDbContext context)
    {
        var mine = CreateUser("user-plan-mine");
        var theirs = CreateUser("user-plan-theirs");
        var squat = new Exercise { Name = "Back Squat", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Legs" };
        var run = new Exercise { Name = "Running", Category = "Cardio", Equipment = "None", MuscleGroups = "Legs" };

        context.Users.AddRange(mine, theirs);
        context.Exercises.AddRange(squat, run);
        await context.SaveChangesAsync();

        return new Fixture(mine.Id, theirs.Id, squat.Id, run.Id);
    }

    private sealed record Fixture(string Mine, string Theirs, int SquatId, int RunId);

    private static ApplicationUser CreateUser(string id) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}
