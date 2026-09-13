namespace CloudManager.Infrastructure.Jobs;

using CloudManager.Host.Infrastructure.Jobs;

using Mofucat.JobScheduler;

public sealed class LocalJobAdapterTests
{
    private static readonly TimeSpan Jst = TimeSpan.FromHours(9);

    // Cron is evaluated against local time fields
    [Theory]
    [InlineData("0 9 * * *", 9, 0, true)]
    [InlineData("0 9 * * *", 9, 1, false)]
    [InlineData("0 9 * * *", 0, 0, false)]
    [InlineData("*/15 * * * *", 10, 30, true)]
    [InlineData("*/15 * * * *", 10, 31, false)]
    public void IsMatchEvaluatesLocalClock(string cron, int hour, int minute, bool expected)
    {
        var time = new DateTimeOffset(2026, 1, 5, hour, minute, 0, Jst);

        Assert.Equal(expected, LocalJobAdapter.IsMatch(CronExpression.Parse(cron), time));
    }

    [Fact]
    public void ToLocalMinuteTruncatesSeconds()
    {
        var time = new DateTimeOffset(2026, 1, 5, 9, 30, 45, Jst).AddMilliseconds(123);

        var minute = LocalJobAdapter.ToLocalMinute(time);

        Assert.Equal(0, minute.Second);
        Assert.Equal(0, minute.Millisecond);
        Assert.Equal(time.ToUniversalTime().AddSeconds(-45).AddMilliseconds(-123), minute.ToUniversalTime());
    }
}
