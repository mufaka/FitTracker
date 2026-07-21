using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;

namespace FitTracker.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAnalyticsService _analyticsService;
    private readonly ITemplateService _templateService;
    private readonly IWorkoutSuggestionService _workoutSuggestionService;
    private readonly IChallengeService _challengeService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAnalyticsService analyticsService,
        ITemplateService templateService,
        IWorkoutSuggestionService workoutSuggestionService,
        IChallengeService challengeService,
        ILogger<IndexModel> logger)
    {
        _context = context;
        _userManager = userManager;
        _analyticsService = analyticsService;
        _templateService = templateService;
        _workoutSuggestionService = workoutSuggestionService;
        _challengeService = challengeService;
        _logger = logger;
    }

    public int WorkoutsThisWeek { get; set; }
    public int CurrentStreak { get; set; }
    public List<Workout> RecentWorkouts { get; set; } = new();
    public Workout? TodaysWorkout { get; set; }
    public DailySummary? TodaysSummary { get; set; }
    public List<WorkoutTemplate> ActiveTemplates { get; set; } = new();
    public WorkoutSuggestionSummary WorkoutSuggestions { get; set; } = new();
    public List<ChallengeProgressItem> ActiveChallenges { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return;

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        // Get workouts this week
        WorkoutsThisWeek = await _context.Workouts
            .Where(w => w.UserId == userId && w.Date >= weekStart && w.IsCompleted)
            .CountAsync();

        // Get today's workout
        TodaysWorkout = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Date.Date == today);

        // Get recent workouts (last 5)
        RecentWorkouts = await _context.Workouts
            .Include(w => w.WorkoutExercises)
                .ThenInclude(we => we.Exercise)
            .Where(w => w.UserId == userId && w.IsCompleted)
            .OrderByDescending(w => w.Date)
            .Take(5)
            .ToListAsync();

        // Calculate current streak
        CurrentStreak = await CalculateStreakAsync(userId);

        // Get today's summary
        TodaysSummary = await _analyticsService.GetDailySummaryAsync(userId, today);
        ActiveTemplates = await _templateService.GetActiveTemplatesAsync(userId, 3);
        WorkoutSuggestions = await _workoutSuggestionService.GetSuggestionsAsync(userId);
        ActiveChallenges = await _challengeService.GetActiveChallengesAsync(userId);
    }

    private async Task<int> CalculateStreakAsync(string userId)
    {
        var workouts = await _context.Workouts
            .Where(w => w.UserId == userId && w.IsCompleted)
            .OrderByDescending(w => w.Date)
            .Select(w => w.Date.Date)
            .Distinct()
            .ToListAsync();

        if (!workouts.Any())
            return 0;

        var streak = 1;
        var currentDate = workouts[0];

        for (int i = 1; i < workouts.Count; i++)
        {
            var previousDate = workouts[i];
            if ((currentDate - previousDate).Days == 1)
            {
                streak++;
                currentDate = previousDate;
            }
            else
            {
                break;
            }
        }

        return streak;
    }
}
