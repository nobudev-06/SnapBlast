namespace SnapBlast.Services;

/// <summary>Shared file/camera helpers so both game modes pick study material the same way.</summary>
public static class StudyFilePicker
{
    private static readonly FilePickerFileType StudyTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.Android, new[] { "image/*", "application/pdf" } },
        { DevicePlatform.iOS, new[] { "public.image", "com.adobe.pdf" } },
        { DevicePlatform.MacCatalyst, new[] { "public.image", "com.adobe.pdf" } },
        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" } }
    });

    public static Task<FileResult?> PickAsync() =>
        FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choose a study sheet",
            FileTypes = StudyTypes
        });

    /// <summary>Throws FeatureNotSupportedException when there is no camera.</summary>
    public static async Task<FileResult?> CaptureAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            throw new FeatureNotSupportedException();
        }

        return await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
        {
            Title = "Capture a study sheet"
        });
    }
}