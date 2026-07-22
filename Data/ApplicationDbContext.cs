using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FitTracker.Models;

namespace FitTracker.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<WorkoutExercise> WorkoutExercises { get; set; }
    public DbSet<WorkoutTemplate> WorkoutTemplates { get; set; }
    public DbSet<WorkoutTemplateExercise> WorkoutTemplateExercises { get; set; }
    public DbSet<PersonalRecord> PersonalRecords { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<UserChallenge> UserChallenges { get; set; }
    public DbSet<BodyMeasurement> BodyMeasurements { get; set; }
    public DbSet<ProgressPhoto> ProgressPhotos { get; set; }
    public DbSet<Set> Sets { get; set; }
    public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
    public DbSet<WorkoutPlanExercise> WorkoutPlanExercises { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Exercise
        builder.Entity<Exercise>()
            .HasIndex(e => e.Category);

        builder.Entity<Exercise>()
            .HasIndex(e => e.Name);

        // Configure Workout
        builder.Entity<Workout>()
            .HasOne(w => w.User)
            .WithMany(u => u.Workouts)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Workout>()
            .HasIndex(w => w.Date);

        builder.Entity<Workout>()
            .HasIndex(w => w.UserId);

        // Restrict, not Cascade: deleting a plan must never take performed workouts with it. Plans
        // are soft-deleted and never physically removed, so this reference stays valid forever and
        // the restriction is never actually hit (WDM-20).
        builder.Entity<Workout>()
            .HasOne(w => w.WorkoutPlan)
            .WithMany()
            .HasForeignKey(w => w.WorkoutPlanId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure PersonalRecord
        builder.Entity<PersonalRecord>()
            .HasOne(pr => pr.User)
            .WithMany(u => u.PersonalRecords)
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PersonalRecord>()
            .HasOne(pr => pr.Exercise)
            .WithMany(e => e.PersonalRecords)
            .HasForeignKey(pr => pr.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PersonalRecord>()
            .HasOne(pr => pr.Workout)
            .WithMany(w => w.PersonalRecords)
            .HasForeignKey(pr => pr.WorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PersonalRecord>()
            .HasIndex(pr => new { pr.UserId, pr.ExerciseId, pr.Date });

        // Copies of a canonical set weight, so they carry the same precision as the source.
        builder.Entity<PersonalRecord>()
            .Property(pr => pr.Weight)
            .HasPrecision(10, 4);

        builder.Entity<PersonalRecord>()
            .Property(pr => pr.OneRepMax)
            .HasPrecision(10, 4);

        // Configure Achievement
        builder.Entity<Achievement>()
            .HasIndex(achievement => achievement.Name)
            .IsUnique();

        builder.Entity<Achievement>()
            .Property(achievement => achievement.Name)
            .HasMaxLength(100);

        builder.Entity<Achievement>()
            .Property(achievement => achievement.Description)
            .HasMaxLength(500);

        builder.Entity<Achievement>()
            .Property(achievement => achievement.Icon)
            .HasMaxLength(20);

        builder.Entity<Achievement>()
            .Property(achievement => achievement.Criteria)
            .HasMaxLength(100);

        // Configure UserAchievement
        builder.Entity<UserAchievement>()
            .HasOne(userAchievement => userAchievement.User)
            .WithMany(user => user.UserAchievements)
            .HasForeignKey(userAchievement => userAchievement.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserAchievement>()
            .HasOne(userAchievement => userAchievement.Achievement)
            .WithMany(achievement => achievement.UserAchievements)
            .HasForeignKey(userAchievement => userAchievement.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserAchievement>()
            .HasIndex(userAchievement => new { userAchievement.UserId, userAchievement.AchievementId })
            .IsUnique();

        builder.Entity<UserAchievement>()
            .HasIndex(userAchievement => new { userAchievement.UserId, userAchievement.UnlockedDate });

        // Configure Challenge
        builder.Entity<Challenge>()
            .HasIndex(challenge => challenge.Name)
            .IsUnique();

        builder.Entity<Challenge>()
            .Property(challenge => challenge.Name)
            .HasMaxLength(100);

        builder.Entity<Challenge>()
            .Property(challenge => challenge.Description)
            .HasMaxLength(500);

        builder.Entity<Challenge>()
            .Property(challenge => challenge.Icon)
            .HasMaxLength(20);

        builder.Entity<Challenge>()
            .Property(challenge => challenge.GoalType)
            .HasMaxLength(50);

        builder.Entity<Challenge>()
            .Property(challenge => challenge.Goal)
            .HasPrecision(18, 2);

        // Configure UserChallenge
        builder.Entity<UserChallenge>()
            .HasOne(userChallenge => userChallenge.User)
            .WithMany(user => user.UserChallenges)
            .HasForeignKey(userChallenge => userChallenge.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserChallenge>()
            .HasOne(userChallenge => userChallenge.Challenge)
            .WithMany(challenge => challenge.UserChallenges)
            .HasForeignKey(userChallenge => userChallenge.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user is only ever in a challenge once; re-joining reuses the row.
        builder.Entity<UserChallenge>()
            .HasIndex(userChallenge => new { userChallenge.UserId, userChallenge.ChallengeId })
            .IsUnique();

        builder.Entity<UserChallenge>()
            .HasIndex(userChallenge => new { userChallenge.UserId, userChallenge.StartedDate });

        // Configure BodyMeasurement
        builder.Entity<BodyMeasurement>()
            .HasOne(bm => bm.User)
            .WithMany(u => u.BodyMeasurements)
            .HasForeignKey(bm => bm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BodyMeasurement>()
            .HasIndex(bm => new { bm.UserId, bm.Date });

        // Body weight shares the lbs/kg preference and so the same canonical precision as Set.Weight.
        // The circumference fields below stay at (10, 2): they carry a length unit that has no
        // preference anywhere in the app, and are deliberately left unconverted.
        builder.Entity<BodyMeasurement>()
            .Property(bm => bm.Weight)
            .HasPrecision(10, 4);

        builder.Entity<BodyMeasurement>()
            .Property(bm => bm.BodyFatPercentage)
            .HasPrecision(5, 2);

        builder.Entity<BodyMeasurement>()
            .Property(bm => bm.Chest)
            .HasPrecision(10, 2);

        builder.Entity<BodyMeasurement>()
            .Property(bm => bm.Waist)
            .HasPrecision(10, 2);

        builder.Entity<BodyMeasurement>()
            .Property(bm => bm.Arms)
            .HasPrecision(10, 2);

        builder.Entity<BodyMeasurement>()
            .Property(bm => bm.Legs)
            .HasPrecision(10, 2);

        builder.Entity<BodyMeasurement>()
            .Property(bm => bm.Notes)
            .HasMaxLength(500);

        // Configure ProgressPhoto
        builder.Entity<ProgressPhoto>()
            .HasOne(pp => pp.User)
            .WithMany(u => u.ProgressPhotos)
            .HasForeignKey(pp => pp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProgressPhoto>()
            .HasIndex(pp => new { pp.UserId, pp.Date });

        builder.Entity<ProgressPhoto>()
            .Property(pp => pp.PhotoPath)
            .HasMaxLength(260);

        builder.Entity<ProgressPhoto>()
            .Property(pp => pp.ContentType)
            .HasMaxLength(100);

        builder.Entity<ProgressPhoto>()
            .Property(pp => pp.Notes)
            .HasMaxLength(500);

        // Configure WorkoutTemplate
        // The relationship is optional because a built-in template has no owner (WDM-04).
        builder.Entity<WorkoutTemplate>()
            .HasOne(t => t.User)
            .WithMany(u => u.WorkoutTemplates)
            .HasForeignKey(t => t.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkoutTemplate>()
            .HasIndex(t => t.UserId);

        builder.Entity<WorkoutTemplate>()
            .Property(t => t.Name)
            .HasMaxLength(100);

        builder.Entity<WorkoutTemplate>()
            .Property(t => t.Description)
            .HasMaxLength(500);

        builder.Entity<WorkoutTemplate>()
            .Property(t => t.CatalogKey)
            .HasMaxLength(100);

        // Unique across the catalog, and irrelevant to the user-created templates that leave it
        // null. SQLite treats NULLs as distinct in a unique index, so no filter is needed to let
        // every personal template hold null at once.
        builder.Entity<WorkoutTemplate>()
            .HasIndex(t => t.CatalogKey)
            .IsUnique();

        // Configure WorkoutPlan
        builder.Entity<WorkoutPlan>()
            .HasOne(p => p.User)
            .WithMany(u => u.WorkoutPlans)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkoutPlan>()
            .HasIndex(p => p.UserId);

        builder.Entity<WorkoutPlan>()
            .Property(p => p.Name)
            .HasMaxLength(100);

        builder.Entity<WorkoutPlan>()
            .Property(p => p.Description)
            .HasMaxLength(500);

        // Configure WorkoutPlanExercise
        builder.Entity<WorkoutPlanExercise>()
            .HasOne(pe => pe.Plan)
            .WithMany(p => p.Exercises)
            .HasForeignKey(pe => pe.WorkoutPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, as everywhere else an exercise is referenced: removing a movement from the
        // library must not silently rewrite the plans built on it.
        builder.Entity<WorkoutPlanExercise>()
            .HasOne(pe => pe.Exercise)
            .WithMany(e => e.WorkoutPlanExercises)
            .HasForeignKey(pe => pe.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkoutPlanExercise>()
            .Property(pe => pe.Notes)
            .HasMaxLength(300);

        builder.Entity<WorkoutPlanExercise>()
            .Property(pe => pe.TargetDistance)
            .HasPrecision(10, 4);

        // Configure WorkoutExercise
        builder.Entity<WorkoutExercise>()
            .HasOne(we => we.Workout)
            .WithMany(w => w.WorkoutExercises)
            .HasForeignKey(we => we.WorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkoutExercise>()
            .HasOne(we => we.Exercise)
            .WithMany(e => e.WorkoutExercises)
            .HasForeignKey(we => we.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure WorkoutTemplateExercise
        builder.Entity<WorkoutTemplateExercise>()
            .HasOne(te => te.Template)
            .WithMany(t => t.Exercises)
            .HasForeignKey(te => te.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkoutTemplateExercise>()
            .HasOne(te => te.Exercise)
            .WithMany(e => e.WorkoutTemplateExercises)
            .HasForeignKey(te => te.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkoutTemplateExercise>()
            .Property(te => te.Notes)
            .HasMaxLength(300);

        // Canonical kilometres, at the same precision as every other stored measurement.
        builder.Entity<WorkoutTemplateExercise>()
            .Property(te => te.DefaultDistance)
            .HasPrecision(10, 4);

        // Configure Set
        builder.Entity<Set>()
            .HasOne(s => s.WorkoutExercise)
            .WithMany(we => we.Sets)
            .HasForeignKey(s => s.WorkoutExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Canonical kilograms at four decimals, not two: a display-precision round trip through
        // lbs is only stable at four (45 lbs -> 20.4117 kg -> 45.00 lbs, where two decimals give
        // back 44.99). See UnitConverter and WorkoutDomainModelSpecification D2.
        builder.Entity<Set>()
            .Property(s => s.Weight)
            .HasPrecision(10, 4);

        // Canonical kilometres.
        builder.Entity<Set>()
            .Property(s => s.Distance)
            .HasPrecision(10, 4);

        builder.Entity<WorkoutExercise>()
            .Property(we => we.Status)
            .HasMaxLength(20)
            .HasDefaultValue(WorkoutExerciseStatuses.Pending);
    }
}
