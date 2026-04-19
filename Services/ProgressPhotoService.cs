using FitTracker.Data;
using FitTracker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace FitTracker.Services;

public interface IProgressPhotoService
{
    Task<List<ProgressPhoto>> GetPhotosAsync(string userId);
    Task<ProgressPhoto?> GetPhotoAsync(int id, string userId);
    Task<ProgressPhoto?> SavePhotoAsync(string userId, IFormFile file, DateTime date, string? notes);
    Task<bool> DeletePhotoAsync(int id, string userId);
    string GetPhysicalPath(ProgressPhoto photo);
}

public class ProgressPhotoService : IProgressPhotoService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const int MaxImageDimensionPixels = 2048;
    private const int OptimizedJpegQuality = 82;
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProgressPhotoService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public Task<List<ProgressPhoto>> GetPhotosAsync(string userId)
    {
        return _context.ProgressPhotos
            .AsNoTracking()
            .Where(photo => photo.UserId == userId)
            .OrderByDescending(photo => photo.Date)
            .ThenByDescending(photo => photo.Id)
            .ToListAsync();
    }

    public Task<ProgressPhoto?> GetPhotoAsync(int id, string userId)
    {
        return _context.ProgressPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(photo => photo.Id == id && photo.UserId == userId);
    }

    public async Task<ProgressPhoto?> SavePhotoAsync(string userId, IFormFile file, DateTime date, string? notes)
    {
        ValidateUpload(file);

        var fileName = $"{Guid.NewGuid():N}.jpg";
        var relativePath = Path.Combine(SanitizePathSegment(userId), fileName);
        var physicalPath = GetPhysicalPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        await OptimizeAndSavePhotoAsync(file, physicalPath);

        var photo = new ProgressPhoto
        {
            UserId = userId,
            Date = date.Date,
            PhotoPath = relativePath.Replace('\\', '/'),
            ContentType = "image/jpeg",
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };

        _context.ProgressPhotos.Add(photo);
        await _context.SaveChangesAsync();

        return photo;
    }

    public async Task<bool> DeletePhotoAsync(int id, string userId)
    {
        var photo = await _context.ProgressPhotos
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);

        if (photo == null)
            return false;

        _context.ProgressPhotos.Remove(photo);
        await _context.SaveChangesAsync();

        var physicalPath = GetPhysicalPath(photo);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return true;
    }

    public string GetPhysicalPath(ProgressPhoto photo) => GetPhysicalPath(photo.PhotoPath);

    private string GetPhysicalPath(string relativePath)
    {
        var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_environment.ContentRootPath, "App_Data", "ProgressPhotos", normalizedRelativePath);
    }

    private static void ValidateUpload(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("Choose a photo to upload.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("Photos must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Only JPG, PNG, and WebP images are supported.");
    }

    private static async Task OptimizeAndSavePhotoAsync(IFormFile file, string physicalPath)
    {
        try
        {
            await using var inputStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(inputStream);

            image.Mutate(context =>
            {
                context.AutoOrient();

                if (image.Width > MaxImageDimensionPixels || image.Height > MaxImageDimensionPixels)
                {
                    context.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(MaxImageDimensionPixels, MaxImageDimensionPixels)
                    });
                }
            });

            await using var outputStream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder
            {
                Quality = OptimizedJpegQuality
            });
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Upload a valid image file.");
        }
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
    }
}
