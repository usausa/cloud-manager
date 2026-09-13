namespace CloudManager.Host.Infrastructure.Jobs;

using Mofucat.JobScheduler;

// UTC cron job that reloads its definition from the database at execution time
public sealed class JobAdapter : ISchedulerJob
{
    private readonly long jobId;

    private readonly JobService jobService;

    private readonly JobExecutionService jobExecutionService;

    public JobAdapter(long jobId, JobService jobService, JobExecutionService jobExecutionService)
    {
        this.jobId = jobId;
        this.jobService = jobService;
        this.jobExecutionService = jobExecutionService;
    }

    public async ValueTask ExecuteAsync(DateTimeOffset time, CancellationToken cancellationToken)
    {
        var job = await jobService.QueryAsync(jobId, cancellationToken);
        if (job is null || !job.IsEnabled)
        {
            return;
        }

        await jobExecutionService.ExecuteAsync(job, cancellationToken);
    }
}
