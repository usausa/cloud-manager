namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class S3PreviewDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Parameter]
    public S3ObjectInfo? ObjectInfo { get; set; }

    [Inject]
    public required S3Service Service { get; set; }

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

    private static readonly string[] TextExtensions = [".txt", ".json", ".yaml", ".yml", ".xml", ".log", ".csv", ".md"];

    private const long MaxImageBytes = 10 * 1024 * 1024;

    private const long MaxTextBytes = 1 * 1024 * 1024;

    private string previewType = "none";

    private string? imageData;

    private string? textContent;

    protected override Task OnInitializedAsync()
    {
        if (ObjectInfo is null)
        {
            return Task.CompletedTask;
        }

        var ext = Path.GetExtension(ObjectInfo.Key);
        if (ImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase) && ObjectInfo.Size <= MaxImageBytes)
        {
            previewType = "image";
        }
        else if (TextExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase) && ObjectInfo.Size <= MaxTextBytes)
        {
            previewType = "text";
        }
        else
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            var bytes = await Service.DownloadBytesAsync(BucketName, ObjectInfo.Key, null, CancellationToken);
            if (previewType == "image")
            {
                var mime = ext == ".png" ? "image/png" : ext == ".gif" ? "image/gif" : ext == ".webp" ? "image/webp" : "image/jpeg";
                imageData = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            else
            {
                textContent = Encoding.UTF8.GetString(bytes);
            }
        });
    }
}
