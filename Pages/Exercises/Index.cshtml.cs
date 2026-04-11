using Microsoft.AspNetCore.Mvc.RazorPages;
using FitTracker.Models;
using FitTracker.Services;

namespace FitTracker.Pages.Exercises;

public class IndexModel : PageModel
{
    private readonly IExerciseService _exerciseService;

    public IndexModel(IExerciseService exerciseService)
    {
        _exerciseService = exerciseService;
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

        TotalExercises = await _exerciseService.GetTotalExercisesAsync();
        Exercises = await _exerciseService.SearchExercisesAsync(searchTerm, category, equipment);
    }
}
