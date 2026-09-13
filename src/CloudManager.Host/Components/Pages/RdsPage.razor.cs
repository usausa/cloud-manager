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

    private async Task LoadSnapshotsAsync()
    {
        isSnapLoading = true;
        try
        {
            snapshots = await Service.ListSnapshotsAsync(null, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isSnapLoading = false;
        }
    }

    private async Task StartAsync(RdsInstanceInfo instance)
    {
        var result = await DialogService.ShowOperationConfirm("RDS Start", $"DB インスタンス {instance.DbInstanceIdentifier} を起動しますか？");
        if (result is null)
        {
            return;
        }

        await RunAsync("実行中...", async (progress, cancellationToken) =>
        {
            await Service.StartInstanceAsync(instance.DbInstanceIdentifier, wait: true, timeoutSeconds: 600, progress, cancellationToken);
            Snackbar.AddSuccess($"Start 完了: {instance.DbInstanceIdentifier}");
        }, LoadAsync);
    }

    private async Task StopAsync(RdsInstanceInfo instance)
    {
        var result = await DialogService.ShowOperationConfirm("RDS Stop", $"DB インスタンス {instance.DbInstanceIdentifier} を停止しますか？");
        if (result is null)
        {
            return;
        }

        await RunAsync("実行中...", async (progress, cancellationToken) =>
        {
            await Service.StopInstanceAsync(instance.DbInstanceIdentifier, wait: true, timeoutSeconds: 600, progress, cancellationToken);
            Snackbar.AddSuccess($"Stop 完了: {instance.DbInstanceIdentifier}");
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

        await RunAsync("実行中...", async (progress, cancellationToken) =>
        {
            await Service.CreateSnapshotAsync(instance.DbInstanceIdentifier, snapParams.SnapshotId, wait: true, timeoutSeconds: 600, progress, cancellationToken);
            Snackbar.AddSuccess($"スナップショット作成完了: {snapParams.SnapshotId}");
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

        await RunAsync("実行中...", async (progress, cancellationToken) =>
        {
            await Service.RestoreSnapshotAsync(snapshot.SnapshotIdentifier, restoreParams.NewDbInstanceId, restoreParams.InstanceClass, wait: true, timeoutSeconds: 900, progress, cancellationToken);
            Snackbar.AddSuccess($"復元完了: {restoreParams.NewDbInstanceId}");
        }, LoadAsync);
    }

    private async Task DeleteSnapshotAsync(RdsSnapshotInfo snapshot)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "スナップショット削除" },
            { x => x.Message, $"スナップショット {snapshot.SnapshotIdentifier} を削除しますか？" },
            { x => x.RequireConfirmText, snapshot.SnapshotIdentifier }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("スナップショット削除確認", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        await RunAsync("実行中...", async (_, cancellationToken) =>
        {
            await Service.DeleteSnapshotAsync(snapshot.SnapshotIdentifier, cancellationToken);
            Snackbar.AddSuccess($"スナップショット削除完了: {snapshot.SnapshotIdentifier}");
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

    private async Task LoadParamGroupsAsync()
    {
        try
        {
            paramGroups = await ParamGroupService.ListParameterGroupsAsync(CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
    }

    private async Task LoadClustersAsync()
    {
        try
        {
            clusters = await AuroraService.ListClustersAsync(CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
    }

    private async Task ShowParamsAsync(RdsParamGroupInfo group)
    {
        await DialogService.ShowAsync<RdsParamGroupDialog>("パラメータ", new DialogParameters<RdsParamGroupDialog>
        {
            { x => x.GroupName, group.Name }
        });
    }

    private async Task FailoverAsync(RdsInstanceInfo instance)
    {
        if (await DialogService.ShowOperationConfirm("Failover 確認", $"インスタンス {instance.DbInstanceIdentifier} を強制 Failover しますか？", requireConfirmText: instance.DbInstanceIdentifier) is null)
        {
            return;
        }

        await RunAsync("Failover 実行中...", async (_, cancellationToken) =>
        {
            await Service.RebootForFailoverAsync(instance.DbInstanceIdentifier, cancellationToken);
            Snackbar.AddSuccess($"Failover を開始しました: {instance.DbInstanceIdentifier}");
        });
    }

    private async Task FailoverClusterAsync(AuroraClusterInfo cluster)
    {
        if (await DialogService.ShowOperationConfirm("Aurora Failover 確認", $"Aurora クラスタ {cluster.ClusterId} を Failover しますか？", requireConfirmText: cluster.ClusterId) is null)
        {
            return;
        }

        await RunAsync("Aurora Failover 中...", async (_, cancellationToken) =>
        {
            await AuroraService.FailoverClusterAsync(cluster.ClusterId, null, cancellationToken);
            Snackbar.AddSuccess($"Aurora Failover を開始しました: {cluster.ClusterId}");
        });
    }

    private async Task ShowEventsAsync(RdsInstanceInfo instance)
    {
        await DialogService.ShowAsync<RdsEventsDialog>("イベント", new DialogParameters<RdsEventsDialog>
        {
            { x => x.SourceIdentifier, instance.DbInstanceIdentifier }
        });
    }
}
