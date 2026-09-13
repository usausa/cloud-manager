namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Infrastructure.Aws;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class Home
{
    private static readonly ServiceCategory[] Categories =
    [
        new("コンピュート",
        [
            new("EC2", "ec2", Icons.Material.Filled.Computer),
            new("EBS", "ebs", Icons.Material.Filled.Album),
            new("ECS", "ecs", Icons.Material.Filled.AccountTree),
            new("Lambda", "lambda", Icons.Material.Filled.Functions)
        ]),
        new("コンテナ",
        [
            new("ECR", "ecr", Icons.Material.Filled.Inventory2)
        ]),
        new("ストレージ / DB",
        [
            new("S3", "s3", Icons.Material.Filled.Folder),
            new("RDS", "rds", Icons.Material.Filled.Storage),
            new("DynamoDB", "dynamodb", Icons.Material.Filled.TableChart)
        ]),
        new("ネットワーク / 配信",
        [
            new("VPC", "vpc", Icons.Material.Filled.Lan),
            new("Elastic IP", "elastic-ip", Icons.Material.Filled.Language),
            new("CloudFront", "cloudfront", Icons.Material.Filled.Public),
            new("ELB", "elb", Icons.Material.Filled.Balance),
            new("Route53", "route53", Icons.Material.Filled.Dns),
            new("ACM", "acm", Icons.Material.Filled.Lock)
        ]),
        new("API / 統合",
        [
            new("API Gateway", "apigw", Icons.Material.Filled.Api),
            new("EventBridge", "eventbridge", Icons.Material.Filled.EventNote)
        ]),
        new("メッセージング",
        [
            new("SQS", "sqs", Icons.Material.Filled.Mail),
            new("SNS", "sns", Icons.Material.Filled.NotificationsActive)
        ]),
        new("監視",
        [
            new("CloudWatch", "cloudwatch", Icons.Material.Filled.Timeline),
            new("CloudWatch Logs", "cloudwatch-logs", Icons.Material.Filled.Article)
        ]),
        new("セキュリティ / ID",
        [
            new("SSM Parameter Store", "ssm-parameters", Icons.Material.Filled.Tune),
            new("Secrets Manager", "secrets-manager", Icons.Material.Filled.VpnKey),
            new("Cognito", "cognito", Icons.Material.Filled.People)
        ]),
        new("ジョブ",
        [
            new("ジョブ一覧", "jobs", Icons.Material.Filled.EventNote),
            new("実行履歴", "jobs/history", Icons.Material.Filled.History)
        ]),
        new("その他",
        [
            new("Cost (USD)", "cost", Icons.Material.Filled.AttachMoney),
            new("設定", "settings", Icons.Material.Filled.Settings)
        ])
    ];

    private int ec2Running;

    private int ec2Stopped;

    private int rdsAvailable;

    private int rdsStopped;

    private int alarmCount;

    private int ecsClusters;

    private int ecsRunningTasks;

    [Inject]
    public required AwsSession Session { get; set; }

    [Inject]
    public required Ec2Service Ec2Service { get; set; }

    [Inject]
    public required RdsService RdsService { get; set; }

    [Inject]
    public required CloudWatchService CloudWatchService { get; set; }

    [Inject]
    public required EcsService EcsService { get; set; }

    protected override Task OnInitializedAsync() =>
        Session.IsProfileAvailable ? LoadAsync(LoadSummaryAsync) : Task.CompletedTask;

    private async Task LoadSummaryAsync()
    {
        var ec2Task = Ec2Service.ListInstancesAsync(null, null, CancellationToken).AsTask();
        var rdsTask = RdsService.ListInstancesAsync(null, CancellationToken).AsTask();
        var alarmTask = CloudWatchService.ListAlarmsAsync().AsTask();
        var ecsTask = EcsService.ListClustersAsync(CancellationToken).AsTask();
        await Task.WhenAll(ec2Task, rdsTask, alarmTask, ecsTask);

        var instances = await ec2Task;
        ec2Running = instances.Count(static x => x.State == "running");
        ec2Stopped = instances.Count(static x => x.State == "stopped");

        var databases = await rdsTask;
        rdsAvailable = databases.Count(static x => x.Status == "available");
        rdsStopped = databases.Count(static x => x.Status == "stopped");

        alarmCount = (await alarmTask).Count(static x => x.StateValue == "ALARM");

        var clusters = await ecsTask;
        ecsClusters = clusters.Count;
        ecsRunningTasks = clusters.Sum(static x => x.RunningTasksCount);
    }

    private sealed record ServiceCategory(string Name, ServiceItem[] Items);

    private sealed record ServiceItem(string Name, string Href, string Icon);
}
