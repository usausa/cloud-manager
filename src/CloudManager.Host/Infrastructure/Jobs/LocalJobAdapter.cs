namespace CloudManager.Host.Infrastructure.Jobs;

using Mofucat.JobScheduler;

// ローカル時刻 cron 用。スケジューラには毎分で登録し、ローカル時刻が cron に一致した分だけ実行する
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

    // 分単位に丸めたローカル時刻
    public static DateTimeOffset ToLocalMinute(DateTimeOffset time)
    {
        var local = TimeZoneInfo.ConvertTime(time, TimeZoneInfo.Local);
        return new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, local.Offset);
    }

    // その分が cron の発火時刻か。1秒前からの次回発火がその分と一致すれば該当
    public static bool IsMatch(CronExpression cron, DateTimeOffset minute) =>
        cron.GetNextOccurrence(minute.AddSeconds(-1)) == minute;
}
