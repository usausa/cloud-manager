namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ElasticIpPage
{
    [Inject]
    public required ElasticIpService Service { get; set; }

    private List<ElasticIpInfo> addresses = [];

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            addresses = await Service.ListElasticIpsAsync(CancellationToken);
        });

    private async Task AssociateAsync(ElasticIpInfo eip)
    {
        var dialog = await DialogService.ShowAsync<EipAssociateDialog>("Elastic IP 関連付け", new DialogParameters<EipAssociateDialog>
        {
            { x => x.AllocationId, eip.AllocationId }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        var instanceId = (string)result.Data!;
        await RunAsync("関連付け中...", async (_, cancellationToken) =>
        {
            await Service.AssociateElasticIpAsync(eip.AllocationId, instanceId, cancellationToken);
            Snackbar.AddSuccess("Elastic IP を関連付けました");
            await LoadAsync();
        });
    }

    private async Task DisassociateAsync(ElasticIpInfo eip)
    {
        if (await DialogService.ShowOperationConfirm("関連付け解除", $"Elastic IP {eip.PublicIp} の関連付けを解除しますか？") is null)
        {
            return;
        }

        await RunAsync("関連付け解除中...", async (_, cancellationToken) =>
        {
            await Service.DisassociateElasticIpAsync(eip.AssociationId!, cancellationToken);
            Snackbar.AddSuccess("関連付けを解除しました");
            await LoadAsync();
        });
    }
}
