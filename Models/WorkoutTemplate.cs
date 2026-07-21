using System.ComponentModel.DataAnnotations;

namespace FitTracker.Models;

public class WorkoutTemplate
{
    public int Id { get; set; }

    /// <summary>
    /// The owner, or <c>null</c> for a built-in template seeded by the application and visible to
    /// every user (WDM-04). This is the only entity in the model with two legitimate read predicates,
    /// so reads that mean to include built-ins say so explicitly rather than repeating a null check.
    /// </summary>
    public string? UserId { get; set; }

    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// Stable identity for a seeded catalog entry, <c>null</c> for anything a user made (WDM-41).
    /// Seeding inserts by this key, so a template can be added to the catalog later without
    /// disturbing the entries already in an existing database.
    /// </summary>
    [StringLength(100)]
    public string? CatalogKey { get; set; }

    /// <summary>Whether this template was seeded rather than created by a user.</summary>
    public bool IsBuiltIn => UserId == null;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<WorkoutTemplateExercise> Exercises { get; set; } = new List<WorkoutTemplateExercise>();
}
