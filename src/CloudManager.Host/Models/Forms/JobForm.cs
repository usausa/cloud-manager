namespace CloudManager.Host.Models.Forms;

public sealed class JobForm
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ProfileName { get; set; } = string.Empty;

    public string RegionName { get; set; } = string.Empty;

    public JobServiceType ServiceType { get; set; } = JobServiceType.Ec2;

    public JobOperation Operation { get; set; } = JobOperation.Ec2Start;

    public string CronExpression { get; set; } = "0 9 * * 1-5";

    public JobCronTimeZone CronTimeZone { get; set; } = JobCronTimeZone.Local;

    public bool IsEnabled { get; set; } = true;

    // EC2
    public string InstanceId { get; set; } = string.Empty;

    // RDS
    public string DbInstanceId { get; set; } = string.Empty;

    // ECS
    public string Cluster { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public int DesiredCount { get; set; }

    // Lambda
    public string FunctionName { get; set; } = string.Empty;

    public string? Payload { get; set; }

    public string InvocationType { get; set; } = "Event";

    // CloudFront
    public string DistributionId { get; set; } = string.Empty;

    public string Paths { get; set; } = "/*";

    public DateTime CreatedAt { get; set; }
}
