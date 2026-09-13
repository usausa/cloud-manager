namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class S3VersionsDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Parameter]
    public string Key { get; set; } = string.Empty;

    [Inject]
    public required S3Service Service { get; set; }

    private List<S3VersionInfo> versions = [];

    private bool isRestoring;

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            versions = await Service.ListVersionsAsync(BucketName, Key, CancellationToken);
        });

    private async Task RestoreAsync(S3VersionInfo v)
    {
        isRestoring = true;
        try
        {
            await Service.RestoreVersionAsync(BucketName, Key, v.VersionId);
            Snackbar.AddSuccess($"バージョン {v.VersionId} を復元しました");
            versions = await Service.ListVersionsAsync(BucketName, Key, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isRestoring = false;
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
