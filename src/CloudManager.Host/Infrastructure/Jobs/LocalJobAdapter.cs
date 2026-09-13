namespace CloudManager.Host.Infrastructure.Jobs;

using Mofucat.JobScheduler;

// Local time cron job, triggered every minute and run when the local time matches
public sealed class LocalJobAdapter : ISchedulerJob
{
    public const string TriggerCronExpression = "* * * * *";

    private readonly JobAdapter inner;

    private readonly CronExpression localCron;

    private DateTimeOffset? lastFiredAt;

    public LocalJobAdapter(JobAdapter inner, string localCronExpression)
    {
        this.inner = inner;
        localCron = CronExpression.Parse(localCronExpression);
    }

    public ValueTask ExecuteAsync(DateTimeOffset time, CancellationToken cancellationToken)
    {
        var localMinute = ToLocalMinute(time);
        if (!IsMatch(localCron, localMinute) || (lastFiredAt == localMinute))
        {
            return ValueTask.CompletedTask;
        }

        lastFiredAt = localMinute;
        return inner.ExecuteAsync(time, cancellationToken);
    }

    // Local time truncated to the minute
    public static DateTimeOffset ToLocalMinute(DateTimeOffset time)
    {
        var local = TimeZoneInfo.ConvertTime(time, TimeZoneInfo.Local);
        return new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, local.Offset);
    }

    // The minute matches when the next occurrence from one second earlier equals it
    public static bool IsMatch(CronExpression cron, DateTimeOffset minute) =>
        cron.GetNextOccurrence(minute.AddSeconds(-1)) == minute;
}
