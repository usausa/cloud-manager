namespace CloudManager.Services.Aws;

using Amazon.CertificateManager.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Acm;

public sealed class AcmService
{
    private readonly AwsClientFactory factory;

    public AcmService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<AcmCertificateInfo>> ListCertificatesAsync(CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateAcmClient();
        var results = new List<AcmCertificateInfo>();
        string? nextToken = null;
        do
        {
            var response = await client.ListCertificatesAsync(new ListCertificatesRequest { NextToken = nextToken }, cancellationToken);
            var now = DateTime.UtcNow;
            foreach (var cert in response.CertificateSummaryList ?? [])
            {
                int? daysToExpiry = cert.NotAfter.HasValue
                    ? (int)(cert.NotAfter.Value - now).TotalDays
                    : null;
                results.Add(new AcmCertificateInfo(
                    cert.CertificateArn ?? string.Empty,
                    cert.DomainName ?? string.Empty,
                    cert.Status?.Value ?? string.Empty,
                    cert.Type?.Value ?? string.Empty,
                    cert.NotAfter,
                    daysToExpiry));
            }
            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<AcmCertificateDetail> GetCertificateAsync(string arn, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateAcmClient();
        var response = await client.DescribeCertificateAsync(new DescribeCertificateRequest { CertificateArn = arn }, cancellationToken);
        var cert = response.Certificate;
        return new AcmCertificateDetail(
            cert.CertificateArn ?? string.Empty,
            cert.DomainName ?? string.Empty,
            cert.SubjectAlternativeNames ?? [],
            cert.DomainValidationOptions?.FirstOrDefault()?.ValidationMethod?.Value ?? string.Empty,
            cert.Status?.Value ?? string.Empty,
            cert.Issuer ?? string.Empty);
    }
}
