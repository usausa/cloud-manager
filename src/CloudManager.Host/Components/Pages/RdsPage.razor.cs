namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class RdsPage
{
    [Inject]
    public required RdsService Service { get; set; }

    [Inject]
    public required RdsParamGroupService ParamGroupService { get; set; }

    [Inject]
    public required AuroraService AuroraService { get; set; }

    private List<RdsInstanceInfo> instances = [];

    private List<RdsSnapshotInfo> snapshots = [];

    private List<RdsParamGroupInfo> paramGroups = [];

    private List<AuroraClusterInfo> clusters = [];

    private bool isSnapLoading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            instances = await Service.ListInstancesAsync(null, CancellationToken);
        });

    private Task LoadSnapshotsAsync() =>
        LoadAsync(async () =>
        {
            snapshots = await Service.ListSnapshotsAsync(null, CancellationToken);
        }, x => isSnapLoading = x);

    private async Task StartAsync(RdsInstanceInfo instance)
    {
        var result = await DialogService.ShowOperationConfirm("起動", $"DB インスタンス {instance.DbInstanceIdentifier} を起動しますか？");
        if (result is null)
        {
            return;
        }

        await RunAsync("起動中...", async (progress, cancellationToken) =>
        {
            await Service.StartInstanceAsync(instance.DbInstanceIdentifier, wait: true, timeoutSeconds: 600, progress, cancellationToken);
            Snackbar.AddSuccess($"{instance.DbInstanceIdentifier} を起動しました。");
        }, LoadAsync);
    }

    private async Task StopAsync(RdsInstanceInfo instance)
    {
        var result = await DialogService.ShowOperationConfirm("停止", $"DB インスタンス {instance.DbInstanceIdentifier} を停止しますか？");
        if (result is null)
        {
            return;
        }

        await RunAsync("停止中...", async (progress, cancellationToken) =>
        {
            await Service.StopInstanceAsync(instance.DbInstanceIdentifier, wait: true, timeoutSeconds: 600, progress, cancellationToken);
            Snackbar.AddSuccess($"{instance.DbInstanceIdentifier} を停止しました。");
        }, LoadAsync);
    }

    private async Task CreateSnapshotAsync(RdsInstanceInfo instance)
    {
        var parameters = new DialogParameters<RdsSnapshotCreateDialog>
        {
            { x => x.DbInstanceId, instance.DbInstanceIdentifier }
        };
        var dialog = await DialogService.ShowAsync<RdsSnapshotCreateDialog>("スナップショット作成", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var snapParams = (RdsSnapshotCreateParams)dialogResult.Data!;

        await RunAsync("スナップショット作成中...", async (progress, cancellationToken) =>
        {
            await Service.CreateSnapshotAsync(instance.DbInstanceIdentifier, snapParams.SnapshotId, wait: true, timeoutSeconds: 600, progress, cancellationToken);
            Snackbar.AddSuccess($"{snapParams.SnapshotId} を作成しました。");
        }, LoadAsync);
    }

    private async Task RestoreSnapshotAsync(RdsSnapshotInfo snapshot)
    {
        var parameters = new DialogParameters<RdsSnapshotRestoreDialog>
        {
            { x => x.SnapshotId, snapshot.SnapshotIdentifier }
        };
        var dialog = await DialogService.ShowAsync<RdsSnapshotRestoreDialog>("スナップショット復元", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var restoreParams = (RdsSnapshotRestoreParams)dialogResult.Data!;

        await RunAsync("復元中...", async (progress, cancellationToken) =>
        {
            await Service.RestoreSnapshotAsync(snapshot.SnapshotIdentifier, restoreParams.NewDbInstanceId, restoreParams.InstanceClass, wait: true, timeoutSeconds: 900, progress, cancellationToken);
            Snackbar.AddSuccess($"{restoreParams.NewDbInstanceId} を復元しました。");
        }, LoadAsync);
    }

    private async Task DeleteSnapshotAsync(RdsSnapshotInfo snapshot)
    {
        if (await DialogService.ShowOperationConfirm("スナップショット削除", $"スナップショット {snapshot.SnapshotIdentifier} を削除しますか？", requireConfirmText: snapshot.SnapshotIdentifier) is null)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteSnapshotAsync(snapshot.SnapshotIdentifier, cancellationToken);
            Snackbar.AddSuccess($"{snapshot.SnapshotIdentifier} を削除しました。");
            await LoadSnapshotsAsync();
        }, LoadAsync);
    }

    private static Color RdsStateColor(string state) => state switch
    {
        "available" => Color.Success,
        "stopped" => Color.Default,
        "starting" or "stopping" or "backing-up" => Color.Warning,
        "deleting" or "failed" => Color.Error,
        _ => Color.Default
    };

    private static Color SnapStateColor(string state) => state switch
    {
        "available" => Color.Success,
        "creating" or "restoring" => Color.Warning,
        "failed" or "deleting" => Color.Error,
        _ => Color.Default
    };

    private Task LoadParamGroupsAsync() =>
        LoadAsync(async () =>
        {
            paramGroups = await ParamGroupService.ListParameterGroupsAsync(CancellationToken);
        });

    private Task LoadClustersAsync() =>
        LoadAsync(async () =>
        {
            clusters = await AuroraService.ListClustersAsync(CancellationToken);
        });

    private async Task ShowParamsAsync(RdsParamGroupInfo group)
    {
        await DialogService.ShowAsync<RdsParamGroupDialog>("パラメータ", new DialogParameters<RdsParamGroupDialog>
        {
            { x => x.GroupName, group.Name }
        },
        Styles.LargeDialog);
    }

    private async Task FailoverAsync(RdsInstanceInfo instance)
    {
        if (await DialogService.ShowOperationConfirm("フェイルオーバー", $"インスタンス {instance.DbInstanceIdentifier} を強制フェイルオーバーしますか？", requireConfirmText: instance.DbInstanceIdentifier) is null)
        {
            return;
        }

        await RunAsync("フェイルオーバー中...", async (_, cancellationToken) =>
        {
            await Service.RebootForFailoverAsync(instance.DbInstanceIdentifier, cancellationToken);
            Snackbar.AddSuccess($"{instance.DbInstanceIdentifier} のフェイルオーバーを開始しました。");
        });
    }

    private async Task FailoverClusterAsync(AuroraClusterInfo cluster)
    {
        if (await DialogService.ShowOperationConfirm("フェイルオーバー", $"Aurora クラスタ {cluster.ClusterId} をフェイルオーバーしますか？", requireConfirmText: cluster.ClusterId) is null)
        {
            return;
        }

        await RunAsync("フェイルオーバー中...", async (_, cancellationToken) =>
        {
            await AuroraService.FailoverClusterAsync(cluster.ClusterId, null, cancellationToken);
            Snackbar.AddSuccess($"{cluster.ClusterId} のフェイルオーバーを開始しました。");
        });
    }

    private async Task ShowEventsAsync(RdsInstanceInfo instance)
    {
        await DialogService.ShowAsync<RdsEventsDialog>("イベント", new DialogParameters<RdsEventsDialog>
        {
            { x => x.SourceIdentifier, instance.DbInstanceIdentifier }
        },
        Styles.LargeDialog);
    }
}
