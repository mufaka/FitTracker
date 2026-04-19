using FitTracker.Models;
using FitTracker.Services;
using FitTracker.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace FitTracker.Tests.Services;

public class ProgressPhotoServiceTests
{
    [Fact]
    public async Task SavePhotoAsync_PersistsPhotoAndCreatesPrivateFile()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();
        using var environment = new TestWebHostEnvironment();

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new ProgressPhotoService(context, environment);
        using var stream = CreateImageStream(600, 800, "image/jpeg");
        IFormFile file = CreateFormFile(stream, "progress.jpg", "image/jpeg");

        var photo = await service.SavePhotoAsync(user.Id, file, new DateTime(2026, 4, 20), "Front pose");

        Assert.NotNull(photo);
        Assert.Equal(user.Id, photo!.UserId);
        Assert.Equal("Front pose", photo.Notes);
        Assert.Equal("image/jpeg", photo.ContentType);
        Assert.EndsWith(".jpg", photo.PhotoPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(service.GetPhysicalPath(photo)));
        Assert.Single(await service.GetPhotosAsync(user.Id));
    }

    [Fact]
    public async Task DeletePhotoAsync_RemovesPhotoRecordAndStoredFile()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();
        using var environment = new TestWebHostEnvironment();

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new ProgressPhotoService(context, environment);
        using var stream = CreateImageStream(500, 500, "image/png");
        IFormFile file = CreateFormFile(stream, "progress.png", "image/png");

        var photo = await service.SavePhotoAsync(user.Id, file, DateTime.UtcNow.Date, null);
        var physicalPath = service.GetPhysicalPath(photo!);

        var deleted = await service.DeletePhotoAsync(photo!.Id, user.Id);

        Assert.True(deleted);
        Assert.False(File.Exists(physicalPath));
        Assert.Empty(await service.GetPhotosAsync(user.Id));
    }

    [Fact]
    public async Task SavePhotoAsync_OptimizesLargeUploadsBeforePersisting()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();
        using var environment = new TestWebHostEnvironment();

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new ProgressPhotoService(context, environment);
        using var stream = CreateImageStream(4096, 3072, "image/png");
        IFormFile file = CreateFormFile(stream, "progress.png", "image/png");

        var photo = await service.SavePhotoAsync(user.Id, file, DateTime.UtcNow.Date, null);

        using var optimizedImage = await Image.LoadAsync(service.GetPhysicalPath(photo!));
        Assert.True(optimizedImage.Width <= 2048);
        Assert.True(optimizedImage.Height <= 2048);
        Assert.Equal("image/jpeg", photo!.ContentType);
    }

    [Fact]
    public async Task SavePhotoAsync_ThrowsForInvalidImageContent()
    {
        using var factory = new TestDbContextFactory();
        using var context = factory.CreateContext();
        using var environment = new TestWebHostEnvironment();

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new ProgressPhotoService(context, environment);
        using var stream = new MemoryStream([1, 2, 3, 4, 5, 6]);
        IFormFile file = CreateFormFile(stream, "invalid.jpg", "image/jpeg");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SavePhotoAsync(user.Id, file, DateTime.UtcNow.Date, null));

        Assert.Equal("Upload a valid image file.", exception.Message);
    }

    private static IFormFile CreateFormFile(MemoryStream stream, string fileName, string contentType)
    {
        stream.Position = 0;

        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static MemoryStream CreateImageStream(int width, int height, string contentType)
    {
        var stream = new MemoryStream();
        using var image = new Image<Rgba32>(width, height, new Rgba32(52, 211, 153));

        if (string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            image.Save(stream, new PngEncoder());
        }
        else
        {
            image.Save(stream, new JpegEncoder { Quality = 90 });
        }

        stream.Position = 0;
        return stream;
    }

    private static ApplicationUser CreateUser(string id = "user-photo-1") => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id}@example.com".ToUpperInvariant()
    };
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment, IDisposable
{
    public TestWebHostEnvironment()
    {
        ContentRootPath = Path.Combine(Path.GetTempPath(), "FitTrackerTests", Guid.NewGuid().ToString("N"));
        WebRootPath = Path.Combine(ContentRootPath, "wwwroot");
        Directory.CreateDirectory(ContentRootPath);
        Directory.CreateDirectory(WebRootPath);
        ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
    }

    public string ApplicationName { get; set; } = "FitTracker.Tests";
    public IFileProvider WebRootFileProvider { get; set; }
    public string WebRootPath { get; set; }
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }

    public void Dispose()
    {
        if (Directory.Exists(ContentRootPath))
        {
            Directory.Delete(ContentRootPath, recursive: true);
        }
    }
}
