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
