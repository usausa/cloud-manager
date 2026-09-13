namespace CloudManager.Services.Aws;

using Amazon.ElasticLoadBalancingV2.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Elb;

public sealed class ElbService
{
    private readonly AwsClientFactory factory;

    public ElbService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<ElbInfo>> ListLoadBalancersAsync(CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateElbClient();
        var results = new List<ElbInfo>();
        string? marker = null;
        do
        {
            var request = new DescribeLoadBalancersRequest { Marker = marker };
            var response = await client.DescribeLoadBalancersAsync(request, cancellationToken);
            foreach (var lb in response.LoadBalancers ?? [])
            {
                results.Add(new ElbInfo(
                    lb.LoadBalancerName ?? string.Empty,
                    lb.Type?.Value ?? string.Empty,
                    lb.Scheme?.Value ?? string.Empty,
                    lb.DNSName ?? string.Empty,
                    lb.State?.Code?.Value ?? string.Empty,
                    lb.VpcId ?? string.Empty,
                    lb.LoadBalancerArn ?? string.Empty));
            }
            marker = response.NextMarker;
        }
        while (!String.IsNullOrEmpty(marker));
        return results;
    }

    public async ValueTask<List<TargetGroupInfo>> ListTargetGroupsAsync(string? loadBalancerArn = null, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateElbClient();
        var results = new List<TargetGroupInfo>();
        string? marker = null;
        do
        {
            var request = new DescribeTargetGroupsRequest { Marker = marker };
            if (!String.IsNullOrWhiteSpace(loadBalancerArn))
            {
                request.LoadBalancerArn = loadBalancerArn;
            }
            var response = await client.DescribeTargetGroupsAsync(request, cancellationToken);
            foreach (var tg in response.TargetGroups ?? [])
            {
                results.Add(new TargetGroupInfo(
                    tg.TargetGroupName ?? string.Empty,
                    tg.Protocol?.Value ?? string.Empty,
                    tg.Port.GetValueOrDefault(),
                    tg.TargetType?.Value ?? string.Empty,
                    tg.HealthCheckPath,
                    tg.TargetGroupArn ?? string.Empty));
            }
            marker = response.NextMarker;
        }
        while (!String.IsNullOrEmpty(marker));
        return results;
    }

    public async ValueTask<List<TargetHealthInfo>> ListTargetHealthAsync(string targetGroupArn, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateElbClient();
        var response = await client.DescribeTargetHealthAsync(new DescribeTargetHealthRequest
        {
            TargetGroupArn = targetGroupArn
        },
        cancellationToken);
#pragma warning disable IDE0028
        return (response.TargetHealthDescriptions ?? []).Select(t => new TargetHealthInfo(
            t.Target?.Id ?? string.Empty,
            t.Target?.Port.GetValueOrDefault() ?? 0,
            t.TargetHealth?.State?.Value ?? string.Empty,
            t.TargetHealth?.Reason?.Value,
            t.TargetHealth?.Description)).ToList();
#pragma warning restore IDE0028
    }
}
