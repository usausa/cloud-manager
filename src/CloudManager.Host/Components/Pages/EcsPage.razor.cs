namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class EcsPage
{
    [Inject]
    public required EcsService Service { get; set; }

    private List<EcsClusterInfo> clusters = [];

    private List<EcsServiceInfo> services = [];

    private string? selectedCluster;

    private bool isSvcLoading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            clusters = await Service.ListClustersAsync(CancellationToken);
        });

    private async Task LoadServicesAsync(string? clusterName)
    {
        selectedCluster = clusterName;
        services = [];
        if (String.IsNullOrEmpty(clusterName))
        {
            return;
        }

        isSvcLoading = true;
        try
        {
            services = await Service.ListServicesAsync(clusterName, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isSvcLoading = false;
        }
    }

    private async Task UpdateDesiredCountAsync(EcsServiceInfo svc)
    {
        var parameters = new DialogParameters<EcsDesiredCountDialog>
        {
            { x => x.ServiceName, svc.ServiceName },
            { x => x.CurrentCount, svc.DesiredCount }
        };
        var dialog = await DialogService.ShowAsync<EcsDesiredCountDialog>("希望タスク数変更", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var p = (EcsDesiredCountParams)dialogResult.Data!;

        await RunAsync($"更新中: {svc.ServiceName}", async (_, cancellationToken) =>
        {
            await Service.UpdateServiceDesiredCountAsync(selectedCluster!, svc.ServiceName, p.DesiredCount, cancellationToken);
            Snackbar.AddSuccess($"希望数更新完了: {svc.ServiceName} -> {p.DesiredCount}");
            await LoadServicesAsync(selectedCluster!);
        });
    }

    private static Color EcsStateColor(string state) => state switch
    {
        "ACTIVE" or "available" => Color.Success,
        "INACTIVE" or "DRAINING" => Color.Warning,
        _ => Color.Default
    };

    private async Task ShowTasksAsync(EcsServiceInfo service)
    {
        if (String.IsNullOrEmpty(selectedCluster))
        {
            return;
        }
        await DialogService.ShowAsync<EcsTasksDialog>("タスク一覧", new DialogParameters<EcsTasksDialog>
        {
            { x => x.ClusterName, selectedCluster },
            { x => x.ServiceName, service.ServiceName }
        });
    }

    private async Task ForceRedeployAsync(EcsServiceInfo service)
    {
        if (String.IsNullOrEmpty(selectedCluster))
        {
            return;
        }
        if (await DialogService.ShowOperationConfirm("強制再デプロイ確認", $"サービス {service.ServiceName} を強制再デプロイしますか？", requireConfirmText: service.ServiceName) is null)
        {
            return;
        }

        await RunAsync("再デプロイ中...", async (_, cancellationToken) =>
        {
            await Service.ForceRedeployAsync(selectedCluster, service.ServiceName, cancellationToken);
            Snackbar.AddSuccess($"再デプロイを開始しました: {service.ServiceName}");
        });
    }
}
