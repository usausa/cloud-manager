namespace CloudManager.Services.Aws;

using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.CloudWatchLogs;

public sealed class CloudWatchLogsService
{
    private readonly AwsClientFactory factory;

    public CloudWatchLogsService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<LogGroupInfo>> ListLogGroupsAsync(string? prefix = null, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCloudWatchLogsClient();
        var results = new List<LogGroupInfo>();
        string? nextToken = null;
        do
        {
            var request = new DescribeLogGroupsRequest { NextToken = nextToken };
            if (!String.IsNullOrWhiteSpace(prefix))
            {
                request.LogGroupNamePrefix = prefix;
            }
            var response = await client.DescribeLogGroupsAsync(request, cancellationToken);
            foreach (var g in response.LogGroups ?? [])
            {
                results.Add(new LogGroupInfo(
                    g.LogGroupName ?? string.Empty,
                    g.RetentionInDays,
                    g.StoredBytes.GetValueOrDefault(),
                    g.CreationTime));
            }
            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<List<LogStreamInfo>> ListLogStreamsAsync(string logGroup, int limit = 50, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCloudWatchLogsClient();
        var results = new List<LogStreamInfo>();
        var request = new DescribeLogStreamsRequest
        {
            LogGroupName = logGroup,
            OrderBy = OrderBy.LastEventTime,
            Descending = true,
            Limit = limit
        };
        var response = await client.DescribeLogStreamsAsync(request, cancellationToken);
        foreach (var s in response.LogStreams ?? [])
        {
            results.Add(new LogStreamInfo(
                s.LogStreamName ?? string.Empty,
                s.LastEventTimestamp,
                s.FirstEventTimestamp));
        }
        return results;
    }

    public async ValueTask<List<LogEventInfo>> GetLogEventsAsync(
        string logGroup,
        string logStream,
        DateTime? start = null,
        DateTime? end = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCloudWatchLogsClient();
        var results = new List<LogEventInfo>();
        var request = new GetLogEventsRequest
        {
            LogGroupName = logGroup,
            LogStreamName = logStream,
            Limit = limit,
            StartFromHead = false
        };
        if (start.HasValue)
        {
            request.StartTime = start.Value;
        }
        if (end.HasValue)
        {
            request.EndTime = end.Value;
        }
        var response = await client.GetLogEventsAsync(request, cancellationToken);
        foreach (var ev in response.Events ?? [])
        {
            results.Add(new LogEventInfo(
                ev.Timestamp ?? DateTime.UtcNow,
                ev.Message ?? string.Empty));
        }
        return results;
    }

    public async ValueTask<string> StartQueryAsync(
        string logGroup,
        string query,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCloudWatchLogsClient();
        var request = new StartQueryRequest
        {
            LogGroupName = logGroup,
            QueryString = query,
            StartTime = new DateTimeOffset(start, TimeSpan.Zero).ToUnixTimeSeconds(),
            EndTime = new DateTimeOffset(end, TimeSpan.Zero).ToUnixTimeSeconds()
        };
        var response = await client.StartQueryAsync(request, cancellationToken);
        return response.QueryId ?? string.Empty;
    }

    public async ValueTask<LogQueryResultInfo> GetQueryResultsAsync(string queryId, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCloudWatchLogsClient();
        var response = await client.GetQueryResultsAsync(new GetQueryResultsRequest { QueryId = queryId }, cancellationToken);
        var records = (response.Results ?? []).Select(row =>
            row.ToDictionary(f => f.Field ?? string.Empty, f => f.Value ?? string.Empty)).ToList();
        return new LogQueryResultInfo(response.Status?.Value ?? "Unknown", records);
    }
}
