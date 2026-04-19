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
    public DbSet<BodyMeasurement> BodyMeasurements { get; set; }
    public DbSet<ProgressPhoto> ProgressPhotos { get; set; }
    public DbSet<Set> Sets { get; set; }

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

        builder.Entity<PersonalRecord>()
            .Property(pr => pr.Weight)
            .HasPrecision(10, 2);

        builder.Entity<PersonalRecord>()
            .Property(pr => pr.OneRepMax)
            .HasPrecision(10, 2);

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

        // Configure BodyMeasurement
        builder.Entity<BodyMeasurement>()
            .HasOne(bm => bm.User)
            .WithMany(u => u.BodyMeasurements)
            .HasForeignKey(bm => bm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BodyMeasurement>()
            .HasIndex(bm => new { bm.UserId, bm.Date });

        builder.Entity<BodyMeasurement>()
            .Property(bm => bm.Weight)
            .HasPrecision(10, 2);

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
        builder.Entity<WorkoutTemplate>()
            .HasOne(t => t.User)
            .WithMany(u => u.WorkoutTemplates)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkoutTemplate>()
            .HasIndex(t => t.UserId);

        builder.Entity<WorkoutTemplate>()
            .Property(t => t.Name)
            .HasMaxLength(100);

        builder.Entity<WorkoutTemplate>()
            .Property(t => t.Description)
            .HasMaxLength(500);

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

        // Configure Set
        builder.Entity<Set>()
            .HasOne(s => s.WorkoutExercise)
            .WithMany(we => we.Sets)
            .HasForeignKey(s => s.WorkoutExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Set>()
            .Property(s => s.Weight)
            .HasPrecision(10, 2);
    }
}
