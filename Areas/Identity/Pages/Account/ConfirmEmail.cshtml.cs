#nullable disable

using System.Text;
using FitTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace FitTracker.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ConfirmEmailModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public bool IsSuccess { get; private set; }
    public string StatusMessage { get; private set; } = string.Empty;
    public string ReturnUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(string userId, string code, string returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            StatusMessage = "The email confirmation link is invalid.";
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            StatusMessage = "Unable to load the requested user.";
            return Page();
        }

        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

        IsSuccess = result.Succeeded;
        StatusMessage = result.Succeeded
            ? "Your email has been confirmed."
            : "We couldn't confirm your email with that link.";

        return Page();
    }
}
