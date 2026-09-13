namespace CloudManager.Services;

public sealed class JobExecutionOptions
{
    // Number of execution logs to keep per job
    [Range(1, 100_000)]
    public int LogRetentionCountPerJob { get; set; } = 100;
}
