namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class EcrImagesPage
{
    [Inject]
    public required EcrService Service { get; set; }

    [Parameter]
    public string Repository { get; set; } = string.Empty;

    private List<EcrImageInfo> images = [];

    private string? lifecyclePolicy;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            images = await Service.ListImagesAsync(Repository, CancellationToken);
            lifecyclePolicy = await Service.GetLifecyclePolicyAsync(Repository, CancellationToken);
        });

    private async Task DeleteAsync(EcrImageInfo image)
    {
        var shortDigest = image.Digest.Length > 19 ? image.Digest[^7..] : image.Digest;
        var dialogParams = new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "イメージ削除確認" },
            { x => x.Message, $"イメージ「{image.Tag ?? image.Digest}」を削除します。" },
            { x => x.RequireConfirmText, shortDigest }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("削除確認", dialogParams);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await RunAsync("実行中...", async (_, cancellationToken) =>
        {
            await Service.DeleteImageAsync(Repository, image.Digest, cancellationToken);
            Snackbar.AddSuccess($"イメージ削除完了: {image.Tag ?? image.Digest}");
            await LoadAsync();
        });
    }
}
