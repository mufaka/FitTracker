using Microsoft.AspNetCore.Identity;

namespace FitTracker.Models;

public class ApplicationUser : IdentityUser
{
    public string? PreferredUnits { get; set; } = "lbs";
    public int DefaultRestTimer { get; set; } = 90;
    public string? Goals { get; set; }
    public bool DarkMode { get; set; } = false;
    
    public virtual ICollection<Workout> Workouts { get; set; } = new List<Workout>();
}
