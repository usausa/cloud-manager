namespace CloudManager.Host.Infrastructure.Jobs;

using CloudManager.Models.Jobs;

using Mofucat.JobScheduler;

// Applies job definition changes to both the database and the scheduler
public sealed class JobManager
{
    private readonly ILogger<JobManager> log;

    private readonly JobService jobService;

    private readonly JobExecutionService jobExecutionService;

    private readonly JobScheduler scheduler;

    private readonly IHostApplicationLifetime lifetime;

    public JobManager(
        ILogger<JobManager> log,
        JobService jobService,
        JobExecutionService jobExecutionService,
        JobScheduler scheduler,
        IHostApplicationLifetime lifetime)
    {
        this.log = log;
        this.jobService = jobService;
        this.jobExecutionService = jobExecutionService;
        this.scheduler = scheduler;
        this.lifetime = lifetime;
    }

    // Registers enabled jobs at startup, skipping broken definitions
    public async ValueTask LoadAllAsync(CancellationToken cancellationToken)
    {
        var jobs = await jobService.QueryAllAsync(cancellationToken);
        foreach (var job in jobs.Where(static x => x.IsEnabled))
        {
            try
            {
                Register(job);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.WarnJobRegisterFailed(job.Id, job.Name, job.CronExpression, ex);
            }
        }
    }

    public async ValueTask<long> AddAsync(JobDefinition job, CancellationToken cancellationToken = default)
    {
        var id = await jobService.InsertAsync(job, cancellationToken);
        if (job.IsEnabled)
        {
            Register(job with { Id = id });
        }

        return id;
    }

    public async ValueTask<bool> UpdateAsync(JobDefinition job, CancellationToken cancellationToken = default)
    {
        var updated = await jobService.UpdateAsync(job, cancellationToken);
        scheduler.RemoveJob(JobName(job.Id));
        if (updated && job.IsEnabled)
        {
            Register(job);
        }

        return updated;
    }

    public ValueTask<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        scheduler.RemoveJob(JobName(id));
        return jobService.DeleteAsync(id, cancellationToken);
    }

    // Manual runs are not tied to the caller and stop only with the application
    public ValueTask<string> ExecuteNowAsync(JobDefinition job) =>
        jobExecutionService.ExecuteAsync(job, lifetime.ApplicationStopping);

    public DateTimeOffset? GetNextExecutionTime(JobDefinition job)
    {
        if (!job.IsEnabled)
        {
            return null;
        }

        return scheduler.FindJob(JobName(job.Id))?.NextExecutionTime;
    }

    private void Register(JobDefinition job)
    {
        var adapter = new JobAdapter(job.Id, jobService, jobExecutionService);
        var timeZone = job.CronTimeZone switch
        {
            JobCronTimeZone.Utc => TimeZoneInfo.Utc,
            JobCronTimeZone.Local => TimeZoneInfo.Local,
            _ => throw new InvalidOperationException($"Unsupported cron time zone. timeZone=[{job.CronTimeZone}]")
        };
        scheduler.AddJob(job.CronExpression, adapter, JobName(job.Id), timeZone: timeZone);
    }

    private static string JobName(long id) => $"job-{id}";
}
