namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class S3Page
{
    [Inject]
    public required S3Service Service { get; set; }

    private List<S3BucketInfo> buckets = [];

    protected override Task OnInitializedAsync()
    {
        return LoadBucketsAsync();
    }

    private Task LoadBucketsAsync() =>
        LoadAsync(async () =>
        {
            buckets = await Service.ListBucketsAsync(CancellationToken);
        });

    private async Task ShowPublicAccessAsync(S3BucketInfo bucket)
    {
        await DialogService.ShowAsync<S3PublicAccessDialog>("公開設定", new DialogParameters<S3PublicAccessDialog>
        {
            { x => x.BucketName, bucket.BucketName }
        });
    }

    private async Task ShowLifecycleAsync(S3BucketInfo bucket)
    {
        await DialogService.ShowAsync<S3LifecycleDialog>("ライフサイクルルール", new DialogParameters<S3LifecycleDialog>
        {
            { x => x.BucketName, bucket.BucketName }
        });
    }
}
