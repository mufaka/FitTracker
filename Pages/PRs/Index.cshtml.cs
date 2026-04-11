using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.PRs;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IPersonalRecordService _personalRecordService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IPersonalRecordService personalRecordService, UserManager<ApplicationUser> userManager)
    {
        _personalRecordService = personalRecordService;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public List<IGrouping<int, PersonalRecord>> RecordsByExercise { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var records = await _personalRecordService.GetRecordsAsync(userId, StartDate, EndDate);
        RecordsByExercise = records
            .GroupBy(pr => pr.ExerciseId)
            .OrderBy(group => group.First().Exercise.Name)
            .ToList();

        return Page();
    }
}
