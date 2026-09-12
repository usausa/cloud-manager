namespace CloudManager.Models.Aws.Acm;

public sealed record AcmCertificateDetail(
    string Arn,
    string DomainName,
    IReadOnlyList<string> SubjectAlternativeNames,
    string ValidationMethod,
    string ValidationStatus,
    string Issuer);
