namespace CloudManager.Models.Aws.CloudWatch;

public sealed record CloudWatchDataPoint(DateTime Timestamp, double Value, string Unit);
