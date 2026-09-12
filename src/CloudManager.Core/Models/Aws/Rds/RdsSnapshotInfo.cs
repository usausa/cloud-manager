namespace CloudManager.Models.Aws.Rds;

public sealed record RdsSnapshotInfo(
    string SnapshotIdentifier,
    string DbInstanceIdentifier,
    string Status,
    string Engine,
    string EngineVersion,
    DateTime? CreatedTime,
    int AllocatedStorage);
