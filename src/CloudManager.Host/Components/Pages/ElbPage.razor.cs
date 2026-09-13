namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

public sealed partial class ElbPage
{
    [Inject]
    public required ElbService Service { get; set; }

    private List<ElbInfo> loadBalancers = [];

    private List<TargetGroupInfo> targetGroups = [];

    private List<TargetHealthInfo> targetHealth = [];

    private ElbInfo? selectedLb;

    private TargetGroupInfo? selectedTg;

    private bool isTgLoading;

    private bool isHealthLoading;

    private string searchText = string.Empty;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedLb = null;
        selectedTg = null;
        targetGroups = [];
        targetHealth = [];
        return LoadAsync(async () =>
        {
            loadBalancers = await Service.ListLoadBalancersAsync(CancellationToken);
        });
    }

    private bool FilterFunc(ElbInfo lb) =>
        String.IsNullOrWhiteSpace(searchText) ||
        lb.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        lb.DnsName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private Task ShowTargetGroupsAsync(ElbInfo lb)
    {
        selectedLb = lb;
        selectedTg = null;
        targetHealth = [];
        return LoadAsync(async () =>
        {
            targetGroups = await Service.ListTargetGroupsAsync(lb.Arn, CancellationToken);
        }, x => isTgLoading = x);
    }

    private Task ShowTargetHealthAsync(TargetGroupInfo tg)
    {
        selectedTg = tg;
        return LoadAsync(async () =>
        {
            targetHealth = await Service.ListTargetHealthAsync(tg.Arn, CancellationToken);
        }, x => isHealthLoading = x);
    }
}
