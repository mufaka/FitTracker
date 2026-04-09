using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;

namespace FitTracker.Pages.Analytics;

[Authorize]
public class DailyModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAnalyticsService _analyticsService;

    public DailyModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAnalyticsService analyticsService)
    {
        _context = context;
        _userManager = userManager;
        _analyticsService = analyticsService;
    }

    public DateTime SelectedDate { get; set; }
    public DailySummary Summary { get; set; } = new();
    public List<Workout> DaysWorkouts { get; set; } = new();
    public DateTime WeekStart { get; set; }
    public List<DateTime> WeekWorkoutDays { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? date)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        // Parse selected date or default to today
        if (DateTime.TryParse(date, out var parsedDate))
        {
            SelectedDate = parsedDate.Date;
        }
        else
        {
            SelectedDate = DateTime.UtcNow.Date;
        }

        // Get daily summary
        Summary = await _analyticsService.GetDailySummaryAsync(userId, SelectedDate);

        // Get all workouts for the day
        var dayStart = SelectedDate.Date;
        var dayEnd = dayStart.AddDays(1);

        DaysWorkouts = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .Where(w => w.UserId == userId && 
                       w.Date >= dayStart && 
                       w.Date < dayEnd &&
                       w.IsCompleted)
            .OrderBy(w => w.Date)
            .ToListAsync();

        // Get week context
        WeekStart = SelectedDate.AddDays(-(int)SelectedDate.DayOfWeek);
        var weekEnd = WeekStart.AddDays(7);

        WeekWorkoutDays = await _context.Workouts
            .Where(w => w.UserId == userId && 
                       w.Date >= WeekStart && 
                       w.Date < weekEnd &&
                       w.IsCompleted)
            .Select(w => w.Date.Date)
            .Distinct()
            .ToListAsync();

        return Page();
    }
}
