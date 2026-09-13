namespace CloudManager.Services;

using CloudManager.Accessors;
using CloudManager.Models.Jobs;

public sealed class JobLogService
{
    private readonly JobLogAccessor jobLogAccessor;

    private readonly TimeProvider timeProvider;

    public JobLogService(
        JobLogAccessor jobLogAccessor,
        TimeProvider timeProvider)
    {
        this.jobLogAccessor = jobLogAccessor;
        this.timeProvider = timeProvider;
    }

    public void CreateTable() =>
        jobLogAccessor.Create();

    public ValueTask<List<JobExecutionLogEntity>> QueryByJobAsync(long jobId, int limit, CancellationToken cancellationToken = default) =>
        jobLogAccessor.QueryByJobAsync(jobId, limit, cancellationToken);

    public ValueTask<List<JobExecutionLogEntity>> QueryRecentAsync(int limit, CancellationToken cancellationToken = default) =>
        jobLogAccessor.QueryRecentAsync(limit, cancellationToken);

    // Records the start of an execution and returns the log id
    public ValueTask<long> StartAsync(long jobId, string jobName, CancellationToken cancellationToken = default) =>
        jobLogAccessor.InsertAsync(jobId, jobName, timeProvider.GetLocalNow().DateTime, JobExecutionStatus.Running, cancellationToken);

    public ValueTask<int> FinishAsync(long id, string status, string? message, string? errorDetail, CancellationToken cancellationToken = default) =>
        jobLogAccessor.UpdateAsync(id, timeProvider.GetLocalNow().DateTime, status, message, errorDetail, cancellationToken);

    public ValueTask<int> TrimAsync(long jobId, int retainCount, CancellationToken cancellationToken = default) =>
        jobLogAccessor.TrimAsync(jobId, retainCount, cancellationToken);
}
