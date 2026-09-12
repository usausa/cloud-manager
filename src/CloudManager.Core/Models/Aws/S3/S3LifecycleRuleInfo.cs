namespace CloudManager.Models.Aws.S3;

public sealed record S3LifecycleRuleInfo(
    string Id,
    string Prefix,
    string Status,
    IReadOnlyList<string> Transitions,
    string? Expiration,
    string? NoncurrentExpiration);
