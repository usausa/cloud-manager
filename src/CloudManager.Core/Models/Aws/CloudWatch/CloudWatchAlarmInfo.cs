namespace CloudManager.Models.Aws.CloudWatch;

public sealed record CloudWatchAlarmInfo(
    string AlarmName,
    string StateValue,
    string? Namespace,
    string? MetricName,
    string? ComparisonOperator,
    double? Threshold,
    int? EvaluationPeriods,
    int? Period,
    DateTime? StateUpdatedTimestamp);
