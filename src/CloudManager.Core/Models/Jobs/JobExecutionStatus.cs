namespace CloudManager.Models.Jobs;

// JobExecutionLog.Status の値
public static class JobExecutionStatus
{
    public const string Running = nameof(Running);

    public const string Success = nameof(Success);

    public const string Failure = nameof(Failure);
}
