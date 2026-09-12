namespace CloudManager.Models.Aws.CloudWatchLogs;

public sealed record LogStreamInfo(
    string StreamName,
    DateTime? LastEvent,
    DateTime? FirstEvent);
