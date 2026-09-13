namespace CloudManager.Host.Workers;

using CloudManager.Host.Infrastructure.Jobs;

using Mofucat.JobScheduler;

// Starts and stops the scheduler and registers jobs at startup
public sealed class JobSchedulerWorker : IHostedService
{
    private readonly ILogger<JobSchedulerWorker> log;

    private readonly JobScheduler scheduler;

    private readonly JobManager manager;

    public JobSchedulerWorker(
        ILogger<JobSchedulerWorker> log,
        JobScheduler scheduler,
        JobManager manager)
    {
        this.log = log;
        this.scheduler = scheduler;
        this.manager = manager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        scheduler.JobError += OnJobError;
        await manager.LoadAllAsync(cancellationToken);
        await scheduler.StartAsync();
        log.InfoWorkerStart(nameof(JobSchedulerWorker));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await scheduler.StopAsync(cancellationToken);
        scheduler.JobError -= OnJobError;
        log.InfoWorkerStop(nameof(JobSchedulerWorker));
    }

    private void OnJobError(object? sender, JobErrorEventArgs e) =>
        log.ErrorSchedulerJobError(e.JobName, e.Exception);
}
