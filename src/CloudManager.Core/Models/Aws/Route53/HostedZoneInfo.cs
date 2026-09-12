namespace CloudManager.Models.Aws.Route53;

public sealed record HostedZoneInfo(
    string Id,
    string Name,
    int RecordSetCount,
    bool PrivateZone);
