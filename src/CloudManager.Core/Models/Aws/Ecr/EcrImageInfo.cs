namespace CloudManager.Models.Aws.Ecr;

public sealed record EcrImageInfo(
    string? Tag,
    string Digest,
    DateTime? PushedAt,
    long SizeBytes);
