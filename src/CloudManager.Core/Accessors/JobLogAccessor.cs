namespace CloudManager.Accessors;

[DataAccessor]
public sealed partial class JobLogAccessor
{
    [Execute]
    public partial void Create();

    [Query]
    public partial ValueTask<List<JobExecutionLogEntity>> QueryByJobAsync(long jobId, int limit, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<JobExecutionLogEntity>> QueryRecentAsync(int limit, CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(long jobId, string jobName, DateTime startedAt, string status, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpdateAsync(long id, DateTime finishedAt, string status, string? message, string? errorDetail, CancellationToken cancellationToken);

    // ジョブごとに直近retainCount件だけ残して削除する
    [Execute]
    public partial ValueTask<int> TrimAsync(long jobId, int retainCount, CancellationToken cancellationToken);
}
