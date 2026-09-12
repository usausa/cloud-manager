namespace CloudManager.Models.Aws.Rds;

public sealed record RdsInstanceInfo(
    string DbInstanceIdentifier,
    string Engine,
    string EngineVersion,
    string Status,
    string DbInstanceClass,
    string? Endpoint,
    bool MultiAz);
