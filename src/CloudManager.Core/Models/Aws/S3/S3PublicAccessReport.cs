namespace CloudManager.Models.Aws.S3;

public sealed record S3PublicAccessReport(
    bool BlockPublicAcls,
    bool IgnorePublicAcls,
    bool BlockPublicPolicy,
    bool RestrictPublicBuckets,
    bool? IsBucketPolicyPublic,
    string Verdict);
