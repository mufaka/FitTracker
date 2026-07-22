using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitTracker.Tests.Services;

/// <summary>
/// WDM-TEST-08. <see cref="WorkoutTemplate"/> is the one entity with two legitimate read predicates:
/// reads see built-ins, writes never touch them. These tests pin both halves of that asymmetry,
/// because getting it wrong in either direction is silent — a too-narrow read hides the catalog, a
/// too-wide write lets one user edit what every user sees.
/// </summary>
public class TemplateOwnershipTests
{
    [Fact]
    public async Task GetTemplatesAsync_ReturnsOwnAndBuiltInButNeverAnotherUsersTemplates()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, theirs) = await SeedTwoUsersAsync(context);

        var service = new TemplateService(context);
        var visible = await service.GetTemplatesAsync(mine);

        Assert.Equal(new[] { "Mine", "Built-in" }, visible.Select(t => t.Name).ToArray());
        Assert.DoesNotContain(visible, t => t.UserId == theirs);
    }

    [Theory]
    [InlineData(TemplateOwnership.Personal, "Mine")]
    [InlineData(TemplateOwnership.BuiltIn, "Built-in")]
    public async Task GetTemplatesAsync_HonoursTheOwnershipFilter(string ownership, string expected)
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, _) = await SeedTwoUsersAsync(context);

        var service = new TemplateService(context);
        var visible = await service.GetTemplatesAsync(mine, ownership);

        Assert.Equal(expected, Assert.Single(visible).Name);
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsOnlyOwnerlessTemplatesAndNeedsNoUser()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        await SeedTwoUsersAsync(context);

        var service = new TemplateService(context);
        var catalog = await service.GetCatalogAsync();

        // The catalog page is anonymous, so a single leaked user-owned row is a data disclosure.
        Assert.Equal("Built-in", Assert.Single(catalog).Name);
        Assert.All(catalog, template => Assert.Null(template.UserId));
    }

    [Fact]
    public async Task SaveTemplateAsync_RefusesToEditABuiltInTemplate()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, _) = await SeedTwoUsersAsync(context);
        var builtInId = await context.WorkoutTemplates.Where(t => t.UserId == null).Select(t => t.Id).SingleAsync();

        var service = new TemplateService(context);
        var result = await service.SaveTemplateAsync(mine, new TemplateEditorModel
        {
            Id = builtInId,
            Name = "Hijacked",
            Exercises = { new TemplateExerciseEditorModel { ExerciseId = await AnyExerciseIdAsync(context) } }
        });

        Assert.Null(result);
        Assert.Equal("Built-in", await context.WorkoutTemplates.Where(t => t.Id == builtInId).Select(t => t.Name).SingleAsync());
    }

    [Fact]
    public async Task SaveTemplateAsync_RefusesToEditAnotherUsersTemplate()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, theirs) = await SeedTwoUsersAsync(context);
        var theirTemplateId = await context.WorkoutTemplates.Where(t => t.UserId == theirs).Select(t => t.Id).SingleAsync();

        var service = new TemplateService(context);
        var result = await service.SaveTemplateAsync(mine, new TemplateEditorModel
        {
            Id = theirTemplateId,
            Name = "Hijacked",
            Exercises = { new TemplateExerciseEditorModel { ExerciseId = await AnyExerciseIdAsync(context) } }
        });

        Assert.Null(result);
        Assert.Equal("Theirs", await context.WorkoutTemplates.Where(t => t.Id == theirTemplateId).Select(t => t.Name).SingleAsync());
    }

    [Fact]
    public async Task DeleteTemplateAsync_RefusesBuiltInAndOtherUsersTemplates()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, theirs) = await SeedTwoUsersAsync(context);
        var builtInId = await context.WorkoutTemplates.Where(t => t.UserId == null).Select(t => t.Id).SingleAsync();
        var theirTemplateId = await context.WorkoutTemplates.Where(t => t.UserId == theirs).Select(t => t.Id).SingleAsync();

        var service = new TemplateService(context);

        Assert.False(await service.DeleteTemplateAsync(builtInId, mine));
        Assert.False(await service.DeleteTemplateAsync(theirTemplateId, mine));
        Assert.Equal(3, await context.WorkoutTemplates.CountAsync());
    }

    [Fact]
    public async Task CopyTemplateAsync_ProducesAnOwnedCopyThatCanBeEditedWithoutTouchingTheOriginal()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, _) = await SeedTwoUsersAsync(context);
        var builtIn = await context.WorkoutTemplates.AsNoTracking()
            .Include(t => t.Exercises)
            .SingleAsync(t => t.UserId == null);

        var service = new TemplateService(context);
        var copyId = await service.CopyTemplateAsync(builtIn.Id, mine);

        Assert.NotNull(copyId);

        var copy = await context.WorkoutTemplates.AsNoTracking()
            .Include(t => t.Exercises)
            .SingleAsync(t => t.Id == copyId!.Value);

        Assert.Equal(mine, copy.UserId);
        Assert.False(copy.IsBuiltIn);
        // The catalog key identifies one seeded row; a copy must not claim it, or the next seed
        // run would treat the user's template as the shipped entry.
        Assert.Null(copy.CatalogKey);
        Assert.Equal("Built-in (copy)", copy.Name);
        Assert.Equal(builtIn.Exercises.Count, copy.Exercises.Count);
        Assert.Equal(
            builtIn.Exercises.OrderBy(e => e.Order).Select(e => (e.ExerciseId, e.DefaultSets, e.DefaultReps, e.DefaultDurationSeconds, e.DefaultDistance)),
            copy.Exercises.OrderBy(e => e.Order).Select(e => (e.ExerciseId, e.DefaultSets, e.DefaultReps, e.DefaultDurationSeconds, e.DefaultDistance)));

        // Editing the copy is allowed and leaves the built-in untouched.
        var editor = await service.GetTemplateEditorAsync(copy.Id, mine);
        Assert.NotNull(editor);
        editor!.Name = "My version";
        Assert.NotNull(await service.SaveTemplateAsync(mine, editor));

        Assert.Equal("Built-in", await context.WorkoutTemplates.Where(t => t.Id == builtIn.Id).Select(t => t.Name).SingleAsync());
        Assert.Equal("My version", await context.WorkoutTemplates.Where(t => t.Id == copy.Id).Select(t => t.Name).SingleAsync());
    }

    [Fact]
    public async Task CopyTemplateAsync_RefusesATemplateTheCallerCannotSee()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, theirs) = await SeedTwoUsersAsync(context);
        var theirTemplateId = await context.WorkoutTemplates.Where(t => t.UserId == theirs).Select(t => t.Id).SingleAsync();

        var service = new TemplateService(context);

        Assert.Null(await service.CopyTemplateAsync(theirTemplateId, mine));
    }

    [Fact]
    public async Task GetTemplateEditorAsync_SeesBuiltInsSoTheyCanBePreviewedButFlagsThemAsSuch()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, theirs) = await SeedTwoUsersAsync(context);
        var builtInId = await context.WorkoutTemplates.Where(t => t.UserId == null).Select(t => t.Id).SingleAsync();
        var theirTemplateId = await context.WorkoutTemplates.Where(t => t.UserId == theirs).Select(t => t.Id).SingleAsync();

        var service = new TemplateService(context);

        var builtIn = await service.GetTemplateEditorAsync(builtInId, mine);
        Assert.NotNull(builtIn);
        Assert.True(builtIn!.IsBuiltIn);

        // Wider for built-ins, never wider for another user.
        Assert.Null(await service.GetTemplateEditorAsync(theirTemplateId, mine));
    }

    [Fact]
    public async Task SaveTemplateAsync_StoresAnOptionalPrescriptionIncludingDistanceInCanonicalKilometres()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();

        var (mine, _) = await SeedTwoUsersAsync(context);
        var exerciseId = await AnyExerciseIdAsync(context);

        var service = new TemplateService(context);
        var templateId = await service.SaveTemplateAsync(mine, new TemplateEditorModel
        {
            Name = "Easy run",
            Exercises =
            {
                // A run prescribes a distance and neither sets nor reps.
                new TemplateExerciseEditorModel { ExerciseId = exerciseId, DefaultDistance = 3.11m, DefaultDurationSeconds = 1800 }
            }
        });

        Assert.NotNull(templateId);

        var saved = await context.WorkoutTemplateExercises.AsNoTracking()
            .SingleAsync(te => te.TemplateId == templateId!.Value);

        Assert.Null(saved.DefaultSets);
        Assert.Null(saved.DefaultReps);
        Assert.Equal(1800, saved.DefaultDurationSeconds);
        // Entered as 3.11 miles by an lbs user, stored as kilometres.
        Assert.Equal(5.0051m, saved.DefaultDistance);

        // And it comes back as the number that was typed.
        var editor = await service.GetTemplateEditorAsync(templateId!.Value, mine);
        Assert.Equal(3.11m, editor!.Exercises[0].DefaultDistance);
    }

    private static async Task<int> AnyExerciseIdAsync(ApplicationDbContext context) =>
        await context.Exercises.Select(e => e.Id).FirstAsync();

    /// <summary>
    /// Three templates: one owned by the caller, one by somebody else, one ownerless built-in.
    /// Returns the two user ids.
    /// </summary>
    private static async Task<(string Mine, string Theirs)> SeedTwoUsersAsync(ApplicationDbContext context)
    {
        var mine = CreateUser("user-template-mine");
        var theirs = CreateUser("user-template-theirs");
        var exercise = new Exercise
        {
            Name = "Running",
            Category = "Cardio",
            Equipment = "None",
            MuscleGroups = "Legs"
        };

        context.Users.AddRange(mine, theirs);
        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();

        context.WorkoutTemplates.AddRange(
            new WorkoutTemplate
            {
                UserId = mine.Id,
                Name = "Mine",
                Exercises = { new WorkoutTemplateExercise { ExerciseId = exercise.Id, Order = 1, DefaultSets = 3, DefaultReps = 10 } }
            },
            new WorkoutTemplate
            {
                UserId = theirs.Id,
                Name = "Theirs",
                Exercises = { new WorkoutTemplateExercise { ExerciseId = exercise.Id, Order = 1, DefaultSets = 5, DefaultReps = 5 } }
            },
            new WorkoutTemplate
            {
                UserId = null,
                CatalogKey = "outdoor-easy-run",
                Name = "Built-in",
                Exercises =
                {
                    new WorkoutTemplateExercise { ExerciseId = exercise.Id, Order = 1, DefaultDurationSeconds = 1800, DefaultDistance = 5m }
                }
            });

        await context.SaveChangesAsync();
        return (mine.Id, theirs.Id);
    }

    private static ApplicationUser CreateUser(string id) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}
