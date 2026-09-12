namespace CloudManager.Models.Aws.S3;

public sealed record S3Listing(IReadOnlyList<string> CommonPrefixes, IReadOnlyList<S3ObjectInfo> Objects);
