namespace CloudManager.Host.Infrastructure.Jobs;

using CloudManager.Models.Jobs;

using Mofucat.JobScheduler;

// ジョブ定義の追加/更新/削除をDBとスケジューラの両方へ反映する
public sealed class JobManager
{
    private readonly ILogger<JobManager> log;

    private readonly JobService jobService;

    private readonly JobExecutionService jobExecutionService;

    private readonly JobScheduler scheduler;

    public JobManager(
        ILogger<JobManager> log,
        JobService jobService,
        JobExecutionService jobExecutionService,
        JobScheduler scheduler)
    {
        this.log = log;
        this.jobService = jobService;
        this.jobExecutionService = jobExecutionService;
        this.scheduler = scheduler;
    }

    // 起動時に有効なジョブをスケジューラへ登録する。壊れた定義があっても他は登録する
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

    public ValueTask<string> ExecuteNowAsync(JobDefinition job, CancellationToken cancellationToken = default) =>
        jobExecutionService.ExecuteAsync(job, cancellationToken);

    public DateTimeOffset? GetNextExecutionTime(JobDefinition job)
    {
        if (job.CronTimeZone == JobCronTimeZone.Local)
        {
            // ローカル cron はスケジューラ上は毎分トリガなので自前で計算する
            var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.Local);
            return CronExpression.Parse(job.CronExpression).GetNextOccurrence(localNow);
        }

        return scheduler.FindJob(JobName(job.Id))?.NextExecutionTime;
    }

    private void Register(JobDefinition job)
    {
        var adapter = new JobAdapter(job.Id, jobService, jobExecutionService);
        switch (job.CronTimeZone)
        {
            case JobCronTimeZone.Utc:
                scheduler.AddJob(job.CronExpression, adapter, JobName(job.Id));
                break;
            case JobCronTimeZone.Local:
                scheduler.AddJob(LocalJobAdapter.TriggerCronExpression, new LocalJobAdapter(adapter, job.CronExpression), JobName(job.Id));
                break;
            default:
                throw new InvalidOperationException($"Unsupported cron time zone. timeZone=[{job.CronTimeZone}]");
        }
    }

    private static string JobName(long id) => $"job-{id}";
}
