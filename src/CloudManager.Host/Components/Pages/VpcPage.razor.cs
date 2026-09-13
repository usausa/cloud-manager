namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

public sealed partial class VpcPage
{
    private List<VpcInfo> vpcs = [];

    private VpcInfo? selectedVpc;

    private IReadOnlyList<VpcSubnetInfo> subnets = [];

    private IReadOnlyList<VpcSecurityGroupInfo> securityGroups = [];

    private IReadOnlyList<VpcRouteTableInfo> routeTables = [];

    private IReadOnlyList<VpcIgwInfo> internetGateways = [];

    private IReadOnlyList<VpcNatGwInfo> natGateways = [];

    private bool isDetailLoading;

    [Inject]
    public required VpcService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedVpc = null;
        ClearDetail();
        return LoadAsync(async () =>
        {
            vpcs = await Service.ListVpcsAsync(CancellationToken);
        });
    }

    private void ClearDetail()
    {
        subnets = [];
        securityGroups = [];
        routeTables = [];
        internetGateways = [];
        natGateways = [];
    }

    // Load the details of the selected VPC
    private async Task OnVpcSelectedAsync(VpcInfo? vpc)
    {
        selectedVpc = vpc;
        ClearDetail();
        if (vpc is null)
        {
            return;
        }

        isDetailLoading = true;
        try
        {
            var detail = await Service.GetVpcDetailAsync(vpc.VpcId, CancellationToken);
            subnets = detail.Subnets;
            securityGroups = detail.SecurityGroups;
            routeTables = detail.RouteTables;
            internetGateways = detail.InternetGateways;
            natGateways = detail.NatGateways;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isDetailLoading = false;
        }
    }
}
