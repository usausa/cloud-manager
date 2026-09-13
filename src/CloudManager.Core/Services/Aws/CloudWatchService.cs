namespace CloudManager.Services.Aws;

using Amazon.CloudWatch.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.CloudWatch;

public sealed class CloudWatchService
{
    private readonly AwsClientFactory factory;

    public CloudWatchService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // Gets statistics for a single metric
    public async ValueTask<List<CloudWatchDataPoint>> GetMetricStatisticsAsync(
        string namespaceName,
        string metricName,
        string? dimension,
        DateTime startTime,
        DateTime endTime,
        int periodSeconds,
        string statistic,
        CancellationToken cancellationToken = default)
    {
        using var cw = factory.CreateCloudWatchClient();
        var dimensions = new List<Dimension>();
        if (!String.IsNullOrWhiteSpace(dimension))
        {
            var parts = dimension.Split('=', 2);
            if (parts.Length == 2)
            {
                dimensions.Add(new Dimension { Name = parts[0], Value = parts[1] });
            }
        }

        var response = await cw.GetMetricStatisticsAsync(
            new GetMetricStatisticsRequest
            {
                Namespace = namespaceName,
                MetricName = metricName,
                Dimensions = dimensions,
                StartTime = startTime.ToUniversalTime(),
                EndTime = endTime.ToUniversalTime(),
                Period = periodSeconds,
                Statistics = [statistic]
            },
            cancellationToken);

#pragma warning disable IDE0028
        return response.Datapoints
            .OrderBy(d => d.Timestamp)
            .Select(d =>
            {
                var value = statistic switch
                {
                    "Sum" => d.Sum ?? 0d,
                    "Maximum" => d.Maximum ?? 0d,
                    "Minimum" => d.Minimum ?? 0d,
                    _ => d.Average ?? 0d
                };
                return new CloudWatchDataPoint(d.Timestamp ?? default, value, d.Unit ?? string.Empty);
            })
            .ToList();
#pragma warning restore IDE0028
    }

    // Lists CloudWatch alarms
    public async ValueTask<List<CloudWatchAlarmInfo>> ListAlarmsAsync(CancellationToken cancellationToken = default)
    {
        using var cw = factory.CreateCloudWatchClient();
        var result = new List<CloudWatchAlarmInfo>();
        string? nextToken = null;

        do
        {
            var response = await cw.DescribeAlarmsAsync(
                new DescribeAlarmsRequest { NextToken = nextToken },
                cancellationToken);

            foreach (var a in response.MetricAlarms ?? [])
            {
                result.Add(new CloudWatchAlarmInfo(
                    a.AlarmName,
                    a.StateValue?.Value ?? "UNKNOWN",
                    a.Namespace,
                    a.MetricName,
                    a.ComparisonOperator?.Value,
                    a.Threshold,
                    a.EvaluationPeriods,
                    a.Period,
                    a.StateUpdatedTimestamp));
            }

            nextToken = response.NextToken;
        }
        while (nextToken is not null);

        return result;
    }
}
