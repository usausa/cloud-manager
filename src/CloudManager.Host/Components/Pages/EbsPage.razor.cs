namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class EbsPage
{
    [Inject]
    public required EbsService Service { get; set; }

    private List<EbsVolumeInfo> volumes = [];

    private string stateFilter = string.Empty;

    private IEnumerable<EbsVolumeInfo> FilteredVolumes =>
        String.IsNullOrEmpty(stateFilter) ? volumes : volumes.Where(v => v.State == stateFilter);

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            volumes = await Service.ListVolumesAsync(cancellationToken: CancellationToken);
        });

    private async Task CreateSnapshotAsync(EbsVolumeInfo volume)
    {
        var dialog = await DialogService.ShowAsync<EbsSnapshotDialog>("スナップショット作成", new DialogParameters<EbsSnapshotDialog>
        {
            { x => x.VolumeId, volume.VolumeId }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        var description = (string)result.Data!;
        await RunAsync("スナップショット作成中...", async (_, cancellationToken) =>
        {
            var id = await Service.CreateSnapshotAsync(volume.VolumeId, description, cancellationToken);
            Snackbar.AddSuccess($"スナップショットを作成しました: {id}");
        });
    }

    private async Task AttachAsync(EbsVolumeInfo volume)
    {
        var dialog = await DialogService.ShowAsync<EbsAttachDialog>("ボリュームのアタッチ", new DialogParameters<EbsAttachDialog>
        {
            { x => x.VolumeId, volume.VolumeId }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        var p = (EbsAttachParams)result.Data!;
        await RunAsync("アタッチ中...", async (_, cancellationToken) =>
        {
            await Service.AttachVolumeAsync(volume.VolumeId, p.InstanceId, p.Device, cancellationToken);
            Snackbar.AddSuccess("ボリュームをアタッチしました");
            await LoadAsync();
        });
    }

    private async Task DetachAsync(EbsVolumeInfo volume)
    {
        if (await DialogService.ShowOperationConfirm("デタッチ確認", $"ボリューム {volume.VolumeId} をデタッチしますか？", requireConfirmText: volume.VolumeId) is null)
        {
            return;
        }

        await RunAsync("デタッチ中...", async (_, cancellationToken) =>
        {
            await Service.DetachVolumeAsync(volume.VolumeId, false, cancellationToken);
            Snackbar.AddSuccess("ボリュームをデタッチしました");
            await LoadAsync();
        });
    }

    private static Color StateColor(string state) => state switch
    {
        "available" => Color.Success,
        "in-use" => Color.Primary,
        "creating" => Color.Warning,
        "deleting" or "deleted" => Color.Error,
        _ => Color.Default
    };
}
