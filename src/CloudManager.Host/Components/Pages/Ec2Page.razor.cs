namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class Ec2Page
{
    private const int WaitTimeoutSeconds = 300;

    private static readonly string[] States = ["running", "stopped", "pending", "stopping", "terminated"];

    private List<Ec2InstanceInfo> instances = [];

    private HashSet<Ec2InstanceInfo> selectedItems = [];

    private string? filterState;

    private string filterTag = string.Empty;

    private SsmRunResult? ssmResult;

    [Inject]
    public required Ec2Service Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            instances = await Service.ListInstancesAsync(filterState, filterTag, CancellationToken);
        });

    private async Task StartAsync(Ec2InstanceInfo instance)
    {
        if (await DialogService.ShowOperationConfirm("起動", $"インスタンス {instance.InstanceId} を起動しますか？") is null)
        {
            return;
        }

        await RunAsync("起動中...", async (progress, cancellationToken) =>
        {
            await Service.StartInstancesAsync([instance.InstanceId], wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{instance.InstanceId} を起動しました。");
        }, LoadAsync);
    }

    private async Task StopAsync(Ec2InstanceInfo instance)
    {
        var result = await DialogService.ShowOperationConfirm("停止", $"インスタンス {instance.InstanceId} を停止しますか？", showForce: true);
        if (result is null)
        {
            return;
        }

        await RunAsync("停止中...", async (progress, cancellationToken) =>
        {
            await Service.StopInstancesAsync([instance.InstanceId], result.Force, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{instance.InstanceId} を停止しました。");
        }, LoadAsync);
    }

    private async Task RebootAsync(Ec2InstanceInfo instance)
    {
        if (await DialogService.ShowOperationConfirm("再起動", $"インスタンス {instance.InstanceId} を再起動しますか？") is null)
        {
            return;
        }

        await RunAsync("再起動中...", async (_, cancellationToken) =>
        {
            await Service.RebootInstancesAsync([instance.InstanceId], cancellationToken);
            Snackbar.AddSuccess($"{instance.InstanceId} を再起動しました。");
        }, LoadAsync);
    }

    private async Task TerminateAsync(Ec2InstanceInfo instance)
    {
        var message = $"インスタンス {instance.InstanceId} を終了(削除)します。この操作は取り消せません。";
        if (await DialogService.ShowOperationConfirm("終了", message, requireConfirmText: instance.InstanceId) is null)
        {
            return;
        }

        await RunAsync("終了中...", async (progress, cancellationToken) =>
        {
            await Service.TerminateInstancesAsync([instance.InstanceId], wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{instance.InstanceId} を終了しました。");
        }, LoadAsync);
    }

    private async Task SsmRunAsync(Ec2InstanceInfo instance)
    {
        var dialog = await DialogService.ShowAsync<SsmRunDialog>(
            "SSM コマンド実行",
            new DialogParameters<SsmRunDialog>
            {
                { x => x.InstanceId, instance.InstanceId }
            },
            Styles.MediumDialog);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var parameters = (SsmRunParams)result.Data!;
        await RunAsync("SSM コマンド実行中...", async (progress, cancellationToken) =>
        {
            ssmResult = await Service.SsmRunAsync(instance.InstanceId, parameters.Command, parameters.TimeoutSeconds, progress, cancellationToken);
            if (ssmResult.Status == "Success")
            {
                Snackbar.AddSuccess($"{instance.InstanceId} で SSM コマンドを実行しました。");
            }
            else
            {
                Snackbar.AddWarning($"{instance.InstanceId} の SSM コマンドが {ssmResult.Status} で終了しました。");
            }
        }, LoadAsync);
    }

    private static Color StateColor(string state) => state switch
    {
        "running" => Color.Success,
        "stopped" => Color.Default,
        "pending" or "stopping" => Color.Warning,
        "terminated" => Color.Error,
        _ => Color.Default
    };
}
