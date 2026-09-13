namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

public sealed partial class CloudWatchLogsPage
{
    [Inject]
    public required CloudWatchLogsService Service { get; set; }

    private List<LogGroupInfo> logGroups = [];

    private List<LogStreamInfo> logStreams = [];

    private List<LogEventInfo> logEvents = [];

    private LogGroupInfo? selectedGroup;

    private LogStreamInfo? selectedStream;

    private bool isStreamsLoading;

    private bool isEventsLoading;

    private string? groupPrefix;

    // Logs Insights
    private const string InsightQueryPlaceholder = "fields @timestamp, @message | sort @timestamp desc | limit 20";

    private string insightQuery = InsightQueryPlaceholder;

    private DateTime? insightStart = DateTime.UtcNow.AddDays(-1).Date;

    private DateTime? insightEnd = DateTime.UtcNow.Date;

    private string? insightStatus;

    private List<InsightRow> insightResults = [];

    private List<string> insightColumns = [];

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedGroup = null;
        selectedStream = null;
        logStreams = [];
        logEvents = [];
        return LoadAsync(async () =>
        {
            logGroups = await Service.ListLogGroupsAsync(groupPrefix, CancellationToken);
        });
    }

    private Task OnGroupSelectedAsync(LogGroupInfo? group)
    {
        selectedGroup = group;
        selectedStream = null;
        logEvents = [];
        insightResults = [];
        insightColumns = [];
        insightStatus = null;
        if (group is not null)
        {
            return LoadStreamsAsync();
        }
        return Task.CompletedTask;
    }

    private async Task LoadStreamsAsync()
    {
        if (selectedGroup is null)
        {
            return;
        }
        isStreamsLoading = true;
        try
        {
            logStreams = await Service.ListLogStreamsAsync(selectedGroup.GroupName, cancellationToken: CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isStreamsLoading = false;
        }
    }

    private Task OnStreamSelectedAsync(LogStreamInfo? stream)
    {
        selectedStream = stream;
        if (stream is not null)
        {
            return LoadEventsAsync();
        }
        return Task.CompletedTask;
    }

    private async Task LoadEventsAsync()
    {
        if (selectedGroup is null || selectedStream is null)
        {
            return;
        }
        isEventsLoading = true;
        try
        {
            logEvents = await Service.GetLogEventsAsync(selectedGroup.GroupName, selectedStream.StreamName, cancellationToken: CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isEventsLoading = false;
        }
    }

    private Task RunInsightsQueryAsync()
    {
        if (selectedGroup is null)
        {
            return Task.CompletedTask;
        }

        insightStatus = "Running";
        insightResults = [];
        insightColumns = [];
        return RunAsync("クエリ実行中...", async (_, cancellationToken) =>
        {
            var start = insightStart ?? DateTime.UtcNow.AddDays(-1);
            var end = insightEnd?.AddDays(1) ?? DateTime.UtcNow;
            var queryId = await Service.StartQueryAsync(selectedGroup.GroupName, insightQuery, start, end, cancellationToken);
            LogQueryResultInfo result;
            var attempts = 0;
            do
            {
                await Task.Delay(1500, cancellationToken);
                result = await Service.GetQueryResultsAsync(queryId, cancellationToken);
                insightStatus = result.Status;
                attempts++;
            }
            while (result.Status is "Running" or "Scheduled" && attempts < 30);

#pragma warning disable IDE0028
            insightResults = result.Records.Select(static x => new InsightRow(x)).ToList();
            if (result.Records.Count > 0)
            {
                insightColumns = result.Records[0].Keys.ToList();
            }
#pragma warning restore IDE0028
        });
    }

    private sealed class InsightRow(Dictionary<string, string> data)
    {
        public string Get(string key) => data.GetValueOrDefault(key, "-");
    }
}
