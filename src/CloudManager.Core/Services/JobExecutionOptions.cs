namespace CloudManager.Services;

public sealed class JobExecutionOptions
{
    // ジョブごとに保持する実行履歴の件数
    [Range(1, 100_000)]
    public int LogRetentionCountPerJob { get; set; } = 100;
}
