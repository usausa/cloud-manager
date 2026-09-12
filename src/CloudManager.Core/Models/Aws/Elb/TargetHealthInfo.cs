namespace CloudManager.Models.Aws.Elb;

public sealed record TargetHealthInfo(
    string TargetId,
    int Port,
    string State,
    string? Reason,
    string? Description);
