namespace CloudManager.Models.Aws.Rds;

public sealed record AuroraClusterInfo(
    string ClusterId,
    string Engine,
    string EngineVersion,
    string Status,
    string? Endpoint,
    string? ReaderEndpoint,
    IReadOnlyList<AuroraClusterMember> Members);
