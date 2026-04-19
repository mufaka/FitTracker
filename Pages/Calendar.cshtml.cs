using FitTracker.Data;
using FitTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitTracker.Pages;

[Authorize]
public class CalendarModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CalendarModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public DateTime SelectedMonth { get; set; }
    public DateTime SelectedDate { get; set; }
    public int CompletedWorkoutCount { get; set; }
    public int PlannedWorkoutCount { get; set; }
    public int ActiveDays { get; set; }
    public List<CalendarWeekViewModel> CalendarWeeks { get; set; } = new();
    public List<Workout> SelectedDateWorkouts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? month, string? date)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        SelectedMonth = TryParseMonth(month) ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        SelectedDate = TryParseDate(date)
            ?? (DateTime.UtcNow.Year == SelectedMonth.Year && DateTime.UtcNow.Month == SelectedMonth.Month
                ? DateTime.UtcNow.Date
                : SelectedMonth.Date);

        var monthStart = new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1);
        var monthEndExclusive = monthStart.AddMonths(1);
        var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
        var monthEnd = monthEndExclusive.AddDays(-1);
        var gridEnd = monthEnd.AddDays(6 - (int)monthEnd.DayOfWeek);

        var monthWorkouts = await _context.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == userId &&
                        w.Date >= monthStart &&
                        w.Date < monthEndExclusive)
            .ToListAsync();

        var summariesByDate = monthWorkouts
            .GroupBy(w => w.Date.Date)
            .ToDictionary(
                group => group.Key,
                group => new CalendarDaySummary
                {
                    Date = group.Key,
                    TotalWorkouts = group.Count(),
                    CompletedWorkouts = group.Count(w => w.IsCompleted),
                    PlannedWorkouts = group.Count(w => !w.IsCompleted),
                    TotalDuration = group.Where(w => w.IsCompleted).Sum(w => w.Duration)
                });

        CompletedWorkoutCount = monthWorkouts.Count(w => w.IsCompleted);
        PlannedWorkoutCount = monthWorkouts.Count(w => !w.IsCompleted);
        ActiveDays = summariesByDate.Count;

        SelectedDateWorkouts = await _context.Workouts
            .AsNoTracking()
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Where(w => w.UserId == userId && w.Date.Date == SelectedDate.Date)
            .OrderBy(w => w.IsCompleted)
            .ThenBy(w => w.Date)
            .ToListAsync();

        var allDays = Enumerable.Range(0, (gridEnd - gridStart).Days + 1)
            .Select(offset => gridStart.AddDays(offset))
            .Select(day =>
            {
                summariesByDate.TryGetValue(day.Date, out var summary);

                return new CalendarDayViewModel
                {
                    Date = day.Date,
                    IsCurrentMonth = day.Month == SelectedMonth.Month && day.Year == SelectedMonth.Year,
                    IsToday = day.Date == DateTime.UtcNow.Date,
                    IsSelected = day.Date == SelectedDate.Date,
                    Summary = summary ?? new CalendarDaySummary { Date = day.Date }
                };
            })
            .ToList();

        CalendarWeeks = allDays
            .Chunk(7)
            .Select(days => new CalendarWeekViewModel { Days = days.ToList() })
            .ToList();

        return Page();
    }

    private static DateTime? TryParseMonth(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse($"{value}-01", out var parsedMonth)
            ? new DateTime(parsedMonth.Year, parsedMonth.Month, 1)
            : null;
    }

    private static DateTime? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(value, out var parsedDate)
            ? parsedDate.Date
            : null;
    }
}

public class CalendarWeekViewModel
{
    public List<CalendarDayViewModel> Days { get; set; } = new();
}

public class CalendarDayViewModel
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public bool IsSelected { get; set; }
    public CalendarDaySummary Summary { get; set; } = new();
}

public class CalendarDaySummary
{
    public DateTime Date { get; set; }
    public int TotalWorkouts { get; set; }
    public int CompletedWorkouts { get; set; }
    public int PlannedWorkouts { get; set; }
    public int TotalDuration { get; set; }
}
