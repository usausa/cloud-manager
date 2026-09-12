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

    // ECS クラスター一覧を取得する(ページング対応)。
    public async ValueTask<List<EcsClusterInfo>> ListClustersAsync()
    {
        using var ecs = factory.CreateEcsClient();
        var arns = new List<string>();
        string? nextToken = null;

        do
        {
            var listResponse = await ecs.ListClustersAsync(
                new ListClustersRequest { NextToken = nextToken });
            arns.AddRange(listResponse.ClusterArns);
            nextToken = listResponse.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));

        if (arns.Count == 0)
        {
            return [];
        }

        var descResponse = await ecs.DescribeClustersAsync(
            new DescribeClustersRequest { Clusters = arns });
        return descResponse.Clusters
            .Select(c => new EcsClusterInfo(
                c.ClusterArn,
                c.ClusterName,
                c.Status,
                c.ActiveServicesCount.GetValueOrDefault(),
                c.RunningTasksCount.GetValueOrDefault()))
            .ToList();
    }

    // 指定クラスターのサービス一覧を取得する(ページング対応)。
    public async ValueTask<List<EcsServiceInfo>> ListServicesAsync(string clusterName)
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
                });
            arns.AddRange(listResponse.ServiceArns);
            nextToken = listResponse.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));

        if (arns.Count == 0)
        {
            return [];
        }

        var descResponse = await ecs.DescribeServicesAsync(
            new DescribeServicesRequest
            {
                Cluster = clusterName,
                Services = arns
            });

        return descResponse.Services
            .Select(s => new EcsServiceInfo(
                s.ServiceName,
                s.Status,
                s.DesiredCount.GetValueOrDefault(),
                s.RunningCount.GetValueOrDefault(),
                s.PendingCount.GetValueOrDefault(),
                s.TaskDefinition))
            .ToList();
    }

    // ECS タスクを実行する。FARGATE の場合は subnetId / securityGroupId が必要。
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

        if (string.Equals(launchType, "FARGATE", StringComparison.OrdinalIgnoreCase))
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

    // ECS サービスの希望タスク数を変更する。
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

    // サービスのタスク一覧を取得する。
    public async ValueTask<List<EcsTaskInfo>> ListTasksAsync(string clusterName, string? serviceName)
    {
        using var ecs = factory.CreateEcsClient();
        var arns = new List<string>();
        string? nextToken = null;
        do
        {
            var request = new ListTasksRequest { Cluster = clusterName, NextToken = nextToken };
            if (!string.IsNullOrEmpty(serviceName))
            {
                request.ServiceName = serviceName;
            }
            var listResponse = await ecs.ListTasksAsync(request);
            arns.AddRange(listResponse.TaskArns);
            nextToken = listResponse.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));

        if (arns.Count == 0)
        {
            return [];
        }

        var descResponse = await ecs.DescribeTasksAsync(new DescribeTasksRequest { Cluster = clusterName, Tasks = arns });
        return (descResponse.Tasks ?? []).Select(t => new EcsTaskInfo(
            t.TaskArn,
            t.TaskArn.Split('/').Last(),
            t.DesiredStatus ?? string.Empty,
            t.LastStatus ?? string.Empty,
            t.StartedBy ?? string.Empty,
            t.StartedAt == DateTime.MinValue ? null : t.StartedAt)).ToList();
    }

    // サービスを強制再デプロイする(イメージ更新反映)。
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
