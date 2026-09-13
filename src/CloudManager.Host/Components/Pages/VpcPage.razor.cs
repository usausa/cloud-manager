namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class VpcPage
{
    [Inject]
    public required VpcService Service { get; set; }

    private List<VpcInfo> vpcs = [];

    private List<VpcSubnetInfo> subnets = [];

    private List<VpcSecurityGroupInfo> securityGroups = [];

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            vpcs = await Service.ListVpcsAsync();
            subnets = [];
            securityGroups = [];
        });
}
