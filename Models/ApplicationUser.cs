using Microsoft.AspNetCore.Identity;

namespace FitTracker.Models;

public class ApplicationUser : IdentityUser
{
    public string? PreferredUnits { get; set; } = "lbs";
    public int DefaultRestTimer { get; set; } = 90;
    public string? Goals { get; set; }
    public bool DarkMode { get; set; } = false;
    
    public virtual ICollection<Workout> Workouts { get; set; } = new List<Workout>();
    public virtual ICollection<WorkoutTemplate> WorkoutTemplates { get; set; } = new List<WorkoutTemplate>();
    public virtual ICollection<PersonalRecord> PersonalRecords { get; set; } = new List<PersonalRecord>();
    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    public virtual ICollection<BodyMeasurement> BodyMeasurements { get; set; } = new List<BodyMeasurement>();
    public virtual ICollection<ProgressPhoto> ProgressPhotos { get; set; } = new List<ProgressPhoto>();
}
