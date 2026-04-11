#nullable disable

using System.ComponentModel.DataAnnotations;
using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Areas.Identity.Pages.Account;

[Authorize]
public class SetupModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public SetupModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Preferred units")]
        public string PreferredUnits { get; set; } = "lbs";

        [Range(15, 600)]
        [Display(Name = "Default rest timer (seconds)")]
        public int DefaultRestTimer { get; set; } = 90;

        [Required]
        [StringLength(500)]
        [Display(Name = "Primary goals")]
        public string Goals { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("./Login");
        }

        Input = new InputModel
        {
            PreferredUnits = string.IsNullOrWhiteSpace(user.PreferredUnits) ? "lbs" : user.PreferredUnits,
            DefaultRestTimer = user.DefaultRestTimer > 0 ? user.DefaultRestTimer : 90,
            Goals = user.Goals
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("./Login");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        user.PreferredUnits = Input.PreferredUnits;
        user.DefaultRestTimer = Input.DefaultRestTimer;
        user.Goals = Input.Goals?.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }
}
