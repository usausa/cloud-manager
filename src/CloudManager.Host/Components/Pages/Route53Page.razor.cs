namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class Route53Page
{
    [Inject]
    public required Route53Service Service { get; set; }

    private List<HostedZoneInfo> hostedZones = [];

    private List<RecordSetInfo> recordSets = [];

    private HostedZoneInfo? selectedZone;

    private bool isRecordsLoading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedZone = null;
        recordSets = [];
        return LoadAsync(async () =>
        {
            hostedZones = await Service.ListHostedZonesAsync(CancellationToken);
        });
    }

    private Task OnZoneSelectedAsync(HostedZoneInfo? zone)
    {
        selectedZone = zone;
        recordSets = [];
        if (zone is not null)
        {
            return LoadRecordsAsync();
        }
        return Task.CompletedTask;
    }

    private Task LoadRecordsAsync()
    {
        if (selectedZone is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            recordSets = await Service.ListRecordSetsAsync(selectedZone.Id, CancellationToken);
        }, x => isRecordsLoading = x);
    }

    private async Task OpenUpsertDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<Route53RecordDialog>("レコード追加/更新", new DialogParameters<Route53RecordDialog>(), Styles.MediumDialog);
        var result = await dialog.Result;
        if (result is null || result.Canceled || selectedZone is null)
        {
            return;
        }
        var p = (Route53RecordParams)result.Data!;

        await RunAsync("保存中...", async (_, cancellationToken) =>
        {
            await Service.UpsertRecordAsync(selectedZone.Id, p.Name, p.Type, p.Ttl, p.Values, cancellationToken);
            Snackbar.AddSuccess($"{p.Name} を保存しました。");
            await LoadRecordsAsync();
        });
    }

    private async Task DeleteRecordAsync(RecordSetInfo record)
    {
        if (selectedZone is null)
        {
            return;
        }
        if (await DialogService.ShowOperationConfirm("レコード削除", $"レコード「{record.Name} ({record.Type})」を削除しますか？") is null)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteRecordAsync(
                selectedZone.Id,
                record.Name,
                record.Type,
                (int)(record.Ttl ?? 300),
                record.Records,
                cancellationToken);
            Snackbar.AddSuccess($"{record.Name} を削除しました。");
            await LoadRecordsAsync();
        });
    }
}
