namespace FitTracker.Services;

/// <summary>
/// Filesystem locations the app writes to. Bound from the "Storage" configuration
/// section so a deployment can put writable data somewhere other than the
/// application directory — for example under a systemd StateDirectory — without
/// the paths being baked in for any one operating system.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public static readonly string DefaultProgressPhotosPath =
        Path.Combine("App_Data", "ProgressPhotos");

    /// <summary>
    /// Where progress photos are stored. An absolute path is used as given; a
    /// relative one is resolved against the content root, which keeps the
    /// out-of-the-box behaviour unchanged.
    /// </summary>
    public string ProgressPhotosPath { get; set; } = DefaultProgressPhotosPath;
}
