namespace CloudManager.Models.Aws.S3;

public sealed record S3VersionInfo(
    string VersionId,
    bool IsLatest,
    DateTime LastModified,
    long Size,
    bool IsDeleteMarker);
