namespace CloudManager.Models.Aws.CloudWatchLogs;

public sealed record LogQueryResultInfo(
    string Status,
    IReadOnlyList<Dictionary<string, string>> Records);
