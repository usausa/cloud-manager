namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Infrastructure.Aws;

using Microsoft.AspNetCore.Components;

public sealed partial class CostPage
{
    [Inject]
    public required CostService Service { get; set; }

    [Inject]
    public required AwsSession Session { get; set; }

    private string ec2InstanceType = "t3.medium";

    private string ec2Region = string.Empty;

    private int ec2Hours = 730;

    private CostEstimateResult? ec2Result;

    private string rdsEngine = "postgres";

    private string rdsInstanceClass = "db.t3.medium";

    private string rdsRegion = string.Empty;

    private int rdsHours = 730;

    private CostEstimateResult? rdsResult;

    protected override void OnInitialized()
    {
        ec2Region = Session.Region?.SystemName ?? "us-east-1";
        rdsRegion = Session.Region?.SystemName ?? "us-east-1";
    }

    private Task EstimateEc2Async()
    {
        ec2Result = null;
        return LoadAsync(async () =>
        {
            ec2Result = await Service.EstimateEc2Async(ec2InstanceType, ec2Region, ec2Hours, CancellationToken);
        });
    }

    private Task EstimateRdsAsync()
    {
        rdsResult = null;
        return LoadAsync(async () =>
        {
            rdsResult = await Service.EstimateRdsAsync(rdsEngine, rdsInstanceClass, rdsRegion, rdsHours, CancellationToken);
        });
    }
}
