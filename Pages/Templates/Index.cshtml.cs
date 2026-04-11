using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages.Templates;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ITemplateService _templateService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ITemplateService templateService, UserManager<ApplicationUser> userManager)
    {
        _templateService = templateService;
        _userManager = userManager;
    }

    public List<WorkoutTemplate> Templates { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        Templates = await _templateService.GetTemplatesAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await _templateService.DeleteTemplateAsync(id, userId);
        return RedirectToPage();
    }
}
