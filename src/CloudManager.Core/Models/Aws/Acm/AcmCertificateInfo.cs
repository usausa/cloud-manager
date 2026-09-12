namespace CloudManager.Models.Aws.Acm;

public sealed record AcmCertificateInfo(
    string Arn,
    string DomainName,
    string Status,
    string Type,
    DateTime? NotAfter,
    int? DaysToExpiry);
