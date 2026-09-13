namespace CloudManager.Models.Jobs;

// Values of JobExecutionLog.Status
public static class JobExecutionStatus
{
    public const string Running = nameof(Running);

    public const string Success = nameof(Success);

    public const string Failure = nameof(Failure);
}
