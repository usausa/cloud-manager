namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Aws;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class S3ObjectsPage
{
    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Inject]
    public required S3Service Service { get; set; }

    [Inject]
    public required AwsSession Session { get; set; }

    private List<S3ObjectInfo> objects = [];

    private string prefixFilter = string.Empty;

    protected override Task OnParametersSetAsync()
    {
        objects = [];
        return LoadObjectsAsync();
    }

    private Task LoadObjectsAsync() =>
        LoadAsync(async () =>
        {
            objects = await Service.ListObjectsAsync(BucketName, prefixFilter, CancellationToken);
        });

    private async Task UploadAsync()
    {
        var parameters = new DialogParameters<S3UploadDialog>
        {
            { x => x.BucketName, BucketName }
        };
        var dialog = await DialogService.ShowAsync<S3UploadDialog>("S3 アップロード", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var uploadParams = (S3UploadParams)dialogResult.Data!;

        await RunAsync("アップロード中...", async (progress, cancellationToken) =>
        {
            await using var stream = uploadParams.File.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024, cancellationToken);
            await Service.UploadStreamAsync(BucketName, uploadParams.Key, stream, uploadParams.File.Size, progress, cancellationToken);
            Snackbar.AddSuccess($"アップロード完了: {uploadParams.Key}");
            await LoadObjectsAsync();
        });
    }

    // The download API has no circuit session, so the profile is passed in the URL
    private string DownloadUrl(S3ObjectInfo obj) =>
        $"api/s3/download/{Uri.EscapeDataString(BucketName)}?key={Uri.EscapeDataString(obj.Key)}&profile={Uri.EscapeDataString(Session.ProfileName)}&region={Uri.EscapeDataString(Session.Region?.SystemName ?? string.Empty)}";

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };

    private async Task DeleteObjectAsync(S3ObjectInfo obj)
    {
        if (await DialogService.ShowOperationConfirm("削除確認", $"オブジェクト {obj.Key} を削除しますか？", requireConfirmText: obj.Key.Split('/').Last()) is null)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteObjectAsync(BucketName, obj.Key, cancellationToken);
            Snackbar.AddSuccess($"削除しました: {obj.Key}");
            await LoadObjectsAsync();
        });
    }

    private async Task ShowVersionsAsync(S3ObjectInfo obj)
    {
        await DialogService.ShowAsync<S3VersionsDialog>("バージョン履歴", new DialogParameters<S3VersionsDialog>
        {
            { x => x.BucketName, BucketName },
            { x => x.Key, obj.Key }
        });
    }

    private async Task CopyMoveAsync(S3ObjectInfo obj)
    {
        var dialog = await DialogService.ShowAsync<S3CopyDialog>("コピー / 移動", new DialogParameters<S3CopyDialog>
        {
            { x => x.SrcBucket, BucketName },
            { x => x.SrcKey, obj.Key }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        var p = (S3CopyParams)result.Data!;
        await RunAsync(p.Move ? "移動中..." : "コピー中...", async (_, cancellationToken) =>
        {
            if (p.Move)
            {
                await Service.MoveObjectAsync(BucketName, obj.Key, p.DstBucket, p.DstKey, cancellationToken);
            }
            else
            {
                await Service.CopyObjectAsync(BucketName, obj.Key, p.DstBucket, p.DstKey, cancellationToken);
            }
            Snackbar.AddSuccess(p.Move ? "移動しました" : "コピーしました");
            await LoadObjectsAsync();
        });
    }

    private async Task PreviewAsync(S3ObjectInfo obj)
    {
        await DialogService.ShowAsync<S3PreviewDialog>("プレビュー", new DialogParameters<S3PreviewDialog>
        {
            { x => x.BucketName, BucketName },
            { x => x.ObjectInfo, obj }
        });
    }
}
