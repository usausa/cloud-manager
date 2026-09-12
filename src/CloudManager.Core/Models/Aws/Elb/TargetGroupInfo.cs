namespace CloudManager.Models.Aws.Elb;

public sealed record TargetGroupInfo(
    string Name,
    string Protocol,
    int Port,
    string TargetType,
    string? HealthCheckPath,
    string Arn);
