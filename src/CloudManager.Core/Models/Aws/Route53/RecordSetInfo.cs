namespace CloudManager.Models.Aws.Route53;

public sealed record RecordSetInfo(
    string Name,
    string Type,
    long? Ttl,
    IReadOnlyList<string> Records);
