namespace CloudManager.Models.Aws.CloudFront;

public sealed record CloudFrontDistributionInfo(
    string Id,
    string DomainName,
    string Origins,
    string Status,
    DateTime LastModified);
