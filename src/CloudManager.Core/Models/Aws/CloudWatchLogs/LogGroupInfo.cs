namespace CloudManager.Models.Aws.CloudWatchLogs;

public sealed record LogGroupInfo(
    string GroupName,
    int? RetentionDays,
    long StoredBytes,
    DateTime? Creation);
