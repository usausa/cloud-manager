namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class CloudFrontPage
{
    [Inject]
    public required CloudFrontService Service { get; set; }

    private List<CloudFrontDistributionInfo> distributions = [];

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            distributions = await Service.ListDistributionsAsync(CancellationToken);
        });

    private async Task InvalidateAsync(CloudFrontDistributionInfo dist)
    {
        var parameters = new DialogParameters<CloudFrontInvalidateDialog>
        {
            { x => x.DistributionId, dist.Id }
        };
        var dialog = await DialogService.ShowAsync<CloudFrontInvalidateDialog>("キャッシュ無効化", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var p = (CloudFrontInvalidateParams)dialogResult.Data!;

        await RunAsync("キャッシュ無効化中...", async (_, cancellationToken) =>
        {
            var paths = p.Paths.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            await Service.InvalidateCacheAsync(dist.Id, paths, cancellationToken);
            Snackbar.AddSuccess($"キャッシュ無効化完了: {dist.Id}");
        });
    }

    private static Color CfStateColor(string state) => state switch
    {
        "Deployed" => Color.Success,
        "InProgress" => Color.Warning,
        _ => Color.Default
    };
}
