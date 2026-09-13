namespace CloudManager.Services.Aws;

using Amazon.RDS;
using Amazon.RDS.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Rds;

// RDS event operations
public sealed class RdsEventService
{
    private readonly AwsClientFactory factory;

    public RdsEventService(AwsClientFactory factory) => this.factory = factory;

    public async ValueTask<List<RdsEventInfo>> ListEventsAsync(
        string? sourceIdentifier,
        string? sourceType,
        DateTime? startTime,
        DateTime? endTime,
        CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        var result = new List<RdsEventInfo>();
        string? marker = null;
        do
        {
            var request = new DescribeEventsRequest
            {
                SourceIdentifier = sourceIdentifier,
                SourceType = sourceType is not null ? SourceType.FindValue(sourceType) : null,
                StartTime = startTime,
                EndTime = endTime,
                Marker = marker
            };
            var response = await rds.DescribeEventsAsync(request, cancellationToken);
            foreach (var e in response.Events ?? [])
            {
                result.Add(new RdsEventInfo(
                    e.SourceIdentifier ?? string.Empty,
                    e.Message ?? string.Empty,
                    String.Join(", ", e.EventCategories ?? []),
                    e.Date.GetValueOrDefault()));
            }
            marker = response.Marker;
        }
        while (!String.IsNullOrEmpty(marker));
        return result;
    }
}
