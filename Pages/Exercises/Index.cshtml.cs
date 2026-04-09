using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;

namespace FitTracker.Pages.Exercises;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Exercise> Exercises { get; set; } = new();
    public int TotalExercises { get; set; }
    public string? SearchTerm { get; set; }
    public string? CategoryFilter { get; set; }
    public string? EquipmentFilter { get; set; }

    public async Task OnGetAsync(string? searchTerm, string? category, string? equipment)
    {
        SearchTerm = searchTerm;
        CategoryFilter = category;
        EquipmentFilter = equipment;

        var query = _context.Exercises.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(e => e.Name.Contains(searchTerm) || 
                                    e.MuscleGroups.Contains(searchTerm) ||
                                    e.Description != null && e.Description.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (!string.IsNullOrEmpty(equipment))
        {
            query = query.Where(e => e.Equipment.Contains(equipment));
        }

        TotalExercises = await _context.Exercises.CountAsync();
        Exercises = await query
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }
}
