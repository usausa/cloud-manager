namespace CloudManager.Services.Aws;

using Amazon.ECS;
using Amazon.ECS.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Ecs;

public sealed class EcsService
{
    private readonly AwsClientFactory factory;

    public EcsService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists ECS clusters (paged)
    public async ValueTask<List<EcsClusterInfo>> ListClustersAsync(CancellationToken cancellationToken = default)
    {
        using var ecs = factory.CreateEcsClient();
        var arns = new List<string>();
        string? nextToken = null;

        do
        {
            var listResponse = await ecs.ListClustersAsync(
                new ListClustersRequest { NextToken = nextToken },
                cancellationToken);
            arns.AddRange(listResponse.ClusterArns);
            nextToken = listResponse.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));

        if (arns.Count == 0)
        {
            return [];
        }

        var descResponse = await ecs.DescribeClustersAsync(
            new DescribeClustersRequest { Clusters = arns },
            cancellationToken);
#pragma warning disable IDE0028
        return descResponse.Clusters
            .Select(c => new EcsClusterInfo(
                c.ClusterArn,
                c.ClusterName,
                c.Status,
                c.ActiveServicesCount.GetValueOrDefault(),
                c.RunningTasksCount.GetValueOrDefault()))
            .ToList();
#pragma warning restore IDE0028
    }

    // Lists services in a cluster (paged)
    public async ValueTask<List<EcsServiceInfo>> ListServicesAsync(string clusterName, CancellationToken cancellationToken = default)
    {
        using var ecs = factory.CreateEcsClient();
        var arns = new List<string>();
        string? nextToken = null;

        do
        {
            var listResponse = await ecs.ListServicesAsync(
                new ListServicesRequest
                {
                    Cluster = clusterName,
                    NextToken = nextToken
                },
                cancellationToken);
            arns.AddRange(listResponse.ServiceArns);
            nextToken = listResponse.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));

        if (arns.Count == 0)
        {
            return [];
        }

        var descResponse = await ecs.DescribeServicesAsync(
            new DescribeServicesRequest
            {
                Cluster = clusterName,
                Services = arns
            },
            cancellationToken);

#pragma warning disable IDE0028
        return descResponse.Services
            .Select(s => new EcsServiceInfo(
                s.ServiceName,
                s.Status,
                s.DesiredCount.GetValueOrDefault(),
                s.RunningCount.GetValueOrDefault(),
                s.PendingCount.GetValueOrDefault(),
                s.TaskDefinition))
            .ToList();
#pragma warning restore IDE0028
    }

    // Runs an ECS task; FARGATE requires subnetId and securityGroupId
    public async ValueTask<string> RunTaskAsync(string clusterName, string taskDefinition, string launchType, string? subnetId, string? securityGroupId, bool assignPublicIp, CancellationToken cancellationToken = default)
    {
        using var ecs = factory.CreateEcsClient();
        var request = new RunTaskRequest
        {
            Cluster = clusterName,
            TaskDefinition = taskDefinition,
            LaunchType = launchType,
            Count = 1
        };

        if (String.Equals(launchType, "FARGATE", StringComparison.OrdinalIgnoreCase))
        {
            request.NetworkConfiguration = new NetworkConfiguration
            {
                AwsvpcConfiguration = new AwsVpcConfiguration
                {
                    Subnets = subnetId is not null ? [subnetId] : [],
                    SecurityGroups = securityGroupId is not null ? [securityGroupId] : [],
                    AssignPublicIp = assignPublicIp ? AssignPublicIp.ENABLED : AssignPublicIp.DISABLED
                }
            };
        }

        var response = await ecs.RunTaskAsync(
            request,
            cancellationToken);

        if (response.Failures.Count > 0)
        {
            var failure = response.Failures[0];
            throw new InvalidOperationException($"ECS RunTask failed: {failure.Reason} ({failure.Arn})");
        }

        return response.Tasks[0].TaskArn;
    }

    // Changes the desired task count of a service
    public async ValueTask UpdateServiceDesiredCountAsync(string clusterName, string serviceName, int desiredCount, CancellationToken cancellationToken = default)
    {
        using var ecs = factory.CreateEcsClient();
        await ecs.UpdateServiceAsync(
            new UpdateServiceRequest
            {
                Cluster = clusterName,
                Service = serviceName,
                DesiredCount = desiredCount
            },
            cancellationToken);
    }

    // Lists tasks of a service
    public async ValueTask<List<EcsTaskInfo>> ListTasksAsync(string clusterName, string? serviceName, CancellationToken cancellationToken = default)
    {
        using var ecs = factory.CreateEcsClient();
        var arns = new List<string>();
        string? nextToken = null;
        do
        {
            var request = new ListTasksRequest { Cluster = clusterName, NextToken = nextToken };
            if (!String.IsNullOrEmpty(serviceName))
            {
                request.ServiceName = serviceName;
            }
            var listResponse = await ecs.ListTasksAsync(request, cancellationToken);
            arns.AddRange(listResponse.TaskArns);
            nextToken = listResponse.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));

        if (arns.Count == 0)
        {
            return [];
        }

        var descResponse = await ecs.DescribeTasksAsync(new DescribeTasksRequest { Cluster = clusterName, Tasks = arns }, cancellationToken);
#pragma warning disable IDE0028
        return (descResponse.Tasks ?? []).Select(t => new EcsTaskInfo(
            t.TaskArn,
            t.TaskArn.Split('/').Last(),
            t.DesiredStatus ?? string.Empty,
            t.LastStatus ?? string.Empty,
            t.StartedBy ?? string.Empty,
            t.StartedAt == DateTime.MinValue ? null : t.StartedAt)).ToList();
#pragma warning restore IDE0028
    }

    // Forces a new deployment of a service to pick up image updates
    public async ValueTask ForceRedeployAsync(string clusterName, string serviceName, CancellationToken cancellationToken = default)
    {
        using var ecs = factory.CreateEcsClient();
        await ecs.UpdateServiceAsync(
            new UpdateServiceRequest
            {
                Cluster = clusterName,
                Service = serviceName,
                ForceNewDeployment = true
            },
            cancellationToken);
    }
}
