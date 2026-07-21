using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitTracker.Tests.Services;

/// <summary>
/// WDM-TEST-09. Seeding is the one piece of this feature that runs on every application start, so
/// it has to be idempotent, and it has to fail loudly rather than half-seed.
/// </summary>
public class TemplateCatalogSeedingTests
{
    [Fact]
    public async Task SeedAsync_InsertsTheWholeCatalogOnAnEmptyDatabase()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await DbInitializer.SeedAsync(context);

        var templates = await context.WorkoutTemplates.AsNoTracking().ToListAsync();

        Assert.Equal(TemplateCatalog.Entries.Count, templates.Count);
        Assert.Equal(25, templates.Count);
        Assert.All(templates, template => Assert.Null(template.UserId));
        Assert.All(templates, template => Assert.NotNull(template.CatalogKey));

        Assert.Equal(
            TemplateCatalog.Entries.Select(entry => entry.CatalogKey).OrderBy(key => key),
            templates.Select(template => template.CatalogKey).OrderBy(key => key));
    }

    [Fact]
    public async Task SeedAsync_IsANoOpWhenRunAgainstAFullySeededDatabase()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await DbInitializer.SeedAsync(context);
        var afterFirstRun = await SnapshotAsync(context);

        await DbInitializer.SeedAsync(context);
        await DbInitializer.SeedAsync(context);

        Assert.Equal(afterFirstRun, await SnapshotAsync(context));
    }

    [Fact]
    public async Task SeedAsync_InsertsOnlyTheMissingEntriesAndLeavesExistingOnesUnmodified()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await DbInitializer.SeedAsync(context);

        // Simulate a database seeded by an earlier release: drop one entry, and edit another so we
        // can prove seeding does not overwrite what is already there.
        var dropped = await context.WorkoutTemplates.SingleAsync(t => t.CatalogKey == "gym-push");
        context.WorkoutTemplates.Remove(dropped);

        var edited = await context.WorkoutTemplates.SingleAsync(t => t.CatalogKey == "gym-pull");
        edited.Name = "Edited in place";
        edited.Description = "Should survive re-seeding.";
        await context.SaveChangesAsync();

        await DbInitializer.SeedAsync(context);

        Assert.Equal(TemplateCatalog.Entries.Count, await context.WorkoutTemplates.CountAsync());
        Assert.True(await context.WorkoutTemplates.AnyAsync(t => t.CatalogKey == "gym-push"));

        var survivor = await context.WorkoutTemplates.AsNoTracking().SingleAsync(t => t.CatalogKey == "gym-pull");
        Assert.Equal("Edited in place", survivor.Name);
        Assert.Equal("Should survive re-seeding.", survivor.Description);
    }

    [Fact]
    public async Task EveryCatalogEntryResolvesToASeededExercise()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await DbInitializer.SeedAsync(context);

        var libraryNames = await context.Exercises.Select(e => e.Name).ToListAsync();

        // The catalog resolves exercises by name, and an unresolvable one fails startup. This test
        // is what turns that from a runtime surprise into a build-time one.
        var unresolved = TemplateCatalog.Entries
            .SelectMany(entry => entry.Exercises.Select(exercise => new { entry.CatalogKey, exercise.ExerciseName }))
            .Where(item => !libraryNames.Contains(item.ExerciseName))
            .Select(item => $"{item.CatalogKey} -> {item.ExerciseName}")
            .ToList();

        Assert.Empty(unresolved);
    }

    [Fact]
    public async Task SeedAsync_MaterializesThePrescriptionIncludingDurationAndCanonicalDistance()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await DbInitializer.SeedAsync(context);

        var easyRun = await context.WorkoutTemplates.AsNoTracking()
            .Include(t => t.Exercises).ThenInclude(te => te.Exercise)
            .SingleAsync(t => t.CatalogKey == "outdoor-easy-run");

        var run = Assert.Single(easyRun.Exercises);
        Assert.Equal("Running", run.Exercise.Name);
        Assert.Equal(1800, run.DefaultDurationSeconds);
        // Seed distances are canonical kilometres, so an imperial user sees 3.11 miles.
        Assert.Equal(5m, run.DefaultDistance);
        Assert.Equal(3.11m, UnitConverter.ToDisplayDistance(run.DefaultDistance, "lbs"));
        Assert.Null(run.DefaultSets);
        Assert.Null(run.DefaultReps);
    }

    [Fact]
    public async Task SeedAsync_AddsTheMobilityExercisesTheWarmUpsNeedAndNoneTrackAOneRepMax()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await DbInitializer.SeedAsync(context);

        var mobility = await context.Exercises.AsNoTracking()
            .Where(e => e.Category == "Mobility")
            .ToListAsync();

        Assert.NotEmpty(mobility);
        // A non-Strength category leaves the seed-time IsWeightLoaded rule false, which is right:
        // none of these has a meaningful one-rep max (WDM-44).
        Assert.All(mobility, exercise => Assert.False(exercise.TracksOneRepMax));
        Assert.Contains(mobility, exercise => exercise.Equipment == "Resistance Band");
    }

    [Fact]
    public async Task SeedAsync_ReachesADatabaseSeededBeforeTheseExercisesExisted()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await DbInitializer.SeedAsync(context);

        // Wind the database back to what an install seeded by an earlier release looks like: the
        // original exercise library, no catalog, and none of the additions the catalog depends on.
        // The whole-table AnyAsync() guard on the original library is satisfied and will skip, so
        // the additions have to be inserted per name or catalog seeding fails at startup.
        context.WorkoutTemplateExercises.RemoveRange(context.WorkoutTemplateExercises);
        context.WorkoutTemplates.RemoveRange(context.WorkoutTemplates);
        await context.SaveChangesAsync();

        var addedLater = new[] { "Bodyweight Squat", "Band Pull-Aparts", "Cat-Cow", "Sprint Intervals", "Walking" };
        context.Exercises.RemoveRange(context.Exercises.Where(e => addedLater.Contains(e.Name)));
        await context.SaveChangesAsync();

        Assert.True(await context.Exercises.AnyAsync(), "the original library should still be present");
        Assert.False(await context.Exercises.AnyAsync(e => e.Name == "Band Pull-Aparts"));

        await DbInitializer.SeedAsync(context);

        Assert.All(addedLater, name => Assert.True(context.Exercises.Any(e => e.Name == name), name));
        Assert.Equal(TemplateCatalog.Entries.Count, await context.WorkoutTemplates.CountAsync());
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsTheSeededTemplatesAndNothingUserOwned()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await DbInitializer.SeedAsync(context);

        var user = new ApplicationUser
        {
            Id = "user-catalog-1",
            UserName = "catalog@example.com",
            NormalizedUserName = "CATALOG@EXAMPLE.COM",
            Email = "catalog@example.com",
            NormalizedEmail = "CATALOG@EXAMPLE.COM"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.WorkoutTemplates.Add(new WorkoutTemplate
        {
            UserId = user.Id,
            Name = "My private template",
            Exercises = { new WorkoutTemplateExercise { ExerciseId = await context.Exercises.Select(e => e.Id).FirstAsync(), Order = 1 } }
        });
        await context.SaveChangesAsync();

        var catalog = await new TemplateService(context).GetCatalogAsync();

        Assert.Equal(TemplateCatalog.Entries.Count, catalog.Count);
        Assert.All(catalog, template => Assert.Null(template.UserId));
        Assert.DoesNotContain(catalog, template => template.Name == "My private template");
    }

    private static async Task<List<(string? Key, string Name, string? Description, int ExerciseCount)>> SnapshotAsync(ApplicationDbContext context) =>
        await context.WorkoutTemplates.AsNoTracking()
            .OrderBy(t => t.CatalogKey)
            .Select(t => new ValueTuple<string?, string, string?, int>(t.CatalogKey, t.Name, t.Description, t.Exercises.Count))
            .ToListAsync();
}
