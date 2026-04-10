using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;

namespace FitTracker.Pages.Workouts;

[Authorize]
public class HistoryModel : PageModel
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HistoryModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public List<Workout> Workouts { get; set; } = new();
    public int TotalWorkouts { get; set; }
    public int FilteredWorkouts { get; set; }
    public int TotalPages { get; set; }
    public int PageSize => DefaultPageSize;
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm) || FromDate.HasValue || ToDate.HasValue;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return;

        TotalWorkouts = await _context.Workouts
            .AsNoTracking()
            .CountAsync(w => w.UserId == userId && w.IsCompleted);

        if (FromDate.HasValue && ToDate.HasValue && FromDate.Value.Date > ToDate.Value.Date)
        {
            (FromDate, ToDate) = (ToDate, FromDate);
        }

        var query = _context.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.IsCompleted);

        if (FromDate.HasValue)
        {
            var fromDate = FromDate.Value.Date;
            query = query.Where(w => w.Date >= fromDate);
        }

        if (ToDate.HasValue)
        {
            var toDateExclusive = ToDate.Value.Date.AddDays(1);
            query = query.Where(w => w.Date < toDateExclusive);
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchTerm = SearchTerm.Trim();
            SearchTerm = searchTerm;
            var pattern = $"%{searchTerm}%";

            query = query.Where(w =>
                (w.Notes != null && EF.Functions.Like(w.Notes, pattern)) ||
                w.WorkoutExercises.Any(we => EF.Functions.Like(we.Exercise.Name, pattern)));
        }

        FilteredWorkouts = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(FilteredWorkouts / (double)DefaultPageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Workouts = await query
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Sets)
            .OrderByDescending(w => w.Date)
            .Skip((PageNumber - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToListAsync();

    }
}
