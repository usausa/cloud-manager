namespace CloudManager.Models.Aws.CloudWatchLogs;

public sealed record LogEventInfo(
    DateTime Timestamp,
    string Message,
    string? LogStreamName = null);
