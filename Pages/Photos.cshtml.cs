using System.ComponentModel.DataAnnotations;
using FitTracker.Models;
using FitTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitTracker.Pages;

[Authorize]
public class PhotosModel : PageModel
{
    private readonly IProgressPhotoService _progressPhotoService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PhotosModel(IProgressPhotoService progressPhotoService, UserManager<ApplicationUser> userManager)
    {
        _progressPhotoService = progressPhotoService;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public int? LeftId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RightId { get; set; }

    [BindProperty]
    public ProgressPhotoUploadModel Upload { get; set; } = new();

    public List<ProgressPhoto> Photos { get; set; } = new();
    public ProgressPhoto? LeftPhoto { get; set; }
    public ProgressPhoto? RightPhoto { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await LoadPhotosAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        if (Upload.File == null)
        {
            ModelState.AddModelError("Upload.File", "Select a photo to upload.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPhotosAsync(userId);
            return Page();
        }

        try
        {
            await _progressPhotoService.SavePhotoAsync(userId, Upload.File!, Upload.Date, Upload.Notes);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadPhotosAsync(userId);
            return Page();
        }

        return RedirectToPage("/Photos", new { LeftId, RightId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        await _progressPhotoService.DeletePhotoAsync(id, userId);

        if (LeftId == id)
            LeftId = null;

        if (RightId == id)
            RightId = null;

        return RedirectToPage("/Photos", new { LeftId, RightId });
    }

    public async Task<IActionResult> OnGetImageAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var photo = await _progressPhotoService.GetPhotoAsync(id, userId);
        if (photo == null)
            return NotFound();

        var physicalPath = _progressPhotoService.GetPhysicalPath(photo);
        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        return PhysicalFile(physicalPath, photo.ContentType);
    }

    private async Task LoadPhotosAsync(string userId)
    {
        Photos = await _progressPhotoService.GetPhotosAsync(userId);
        LeftPhoto = LeftId.HasValue ? Photos.FirstOrDefault(photo => photo.Id == LeftId.Value) : Photos.FirstOrDefault();
        RightPhoto = RightId.HasValue ? Photos.FirstOrDefault(photo => photo.Id == RightId.Value) : Photos.Skip(1).FirstOrDefault();
    }
}

public class ProgressPhotoUploadModel
{
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    public IFormFile? File { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
