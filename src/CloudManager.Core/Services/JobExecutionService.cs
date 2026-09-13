namespace CloudManager.Services;

using Amazon.Runtime;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Jobs;
using CloudManager.Services.Aws;

// Executes the AWS operation of a job definition and records the result
public sealed class JobExecutionService
{
    private readonly ILogger<JobExecutionService> log;

    private readonly JobExecutionOptions options;

    private readonly JobLogService jobLogService;

    public JobExecutionService(
        ILogger<JobExecutionService> log,
        JobExecutionOptions options,
        JobLogService jobLogService)
    {
        this.log = log;
        this.options = options;
        this.jobLogService = jobLogService;
    }

    // Returns the resulting JobExecutionStatus
    public async ValueTask<string> ExecuteAsync(JobDefinition job, CancellationToken cancellationToken)
    {
        log.InfoJobStart(job.Id, job.Name, job.Operation);
        var logId = await jobLogService.StartAsync(job.Id, job.Name, cancellationToken);

        string status;
        string? message = null;
        string? errorDetail = null;
        try
        {
            // Jobs run with the profile of the definition, independent of the UI session
            var factory = AwsClientFactory.Create(job.ProfileName, job.RegionName);
            message = await DispatchAsync(factory, job, cancellationToken);
            status = JobExecutionStatus.Success;
            log.InfoJobSuccess(job.Id, job.Name, message);
        }
        catch (OperationCanceledException)
        {
            status = JobExecutionStatus.Failure;
            errorDetail = "Cancelled";
        }
        catch (AmazonServiceException ex)
        {
            status = JobExecutionStatus.Failure;
            errorDetail = $"[{ex.ErrorCode}] {ex.Message}";
            log.WarnJobFailed(job.Id, job.Name, errorDetail, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            status = JobExecutionStatus.Failure;
            errorDetail = ex.ToString();
            log.WarnJobFailed(job.Id, job.Name, ex.Message, ex);
        }

        // Record the result even while shutting down
        await jobLogService.FinishAsync(logId, status, message, errorDetail, CancellationToken.None);
        await jobLogService.TrimAsync(job.Id, options.LogRetentionCountPerJob, CancellationToken.None);

        return status;
    }

    private static async ValueTask<string?> DispatchAsync(AwsClientFactory factory, JobDefinition job, CancellationToken cancellationToken)
    {
        switch (job.Operation)
        {
            case JobOperation.Ec2Start:
            {
                var p = (Ec2InstanceParameters)job.Parameters;
                await new Ec2Service(factory).StartInstancesAsync([p.InstanceId], wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Started: {p.InstanceId}";
            }

            case JobOperation.Ec2Stop:
            {
                var p = (Ec2InstanceParameters)job.Parameters;
                await new Ec2Service(factory).StopInstancesAsync([p.InstanceId], force: false, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Stopped: {p.InstanceId}";
            }

            case JobOperation.Ec2Reboot:
            {
                var p = (Ec2InstanceParameters)job.Parameters;
                await new Ec2Service(factory).RebootInstancesAsync([p.InstanceId], cancellationToken);
                return $"Rebooted: {p.InstanceId}";
            }

            case JobOperation.RdsStart:
            {
                var p = (RdsInstanceParameters)job.Parameters;
                await new RdsService(factory).StartInstanceAsync(p.DbInstanceId, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Started: {p.DbInstanceId}";
            }

            case JobOperation.RdsStop:
            {
                var p = (RdsInstanceParameters)job.Parameters;
                await new RdsService(factory).StopInstanceAsync(p.DbInstanceId, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Stopped: {p.DbInstanceId}";
            }

            case JobOperation.EcsUpdateDesiredCount:
            {
                var p = (EcsDesiredCountParameters)job.Parameters;
                await new EcsService(factory).UpdateServiceDesiredCountAsync(p.Cluster, p.ServiceName, p.DesiredCount, cancellationToken);
                return $"Updated: {p.Cluster}/{p.ServiceName} desired={p.DesiredCount}";
            }

            case JobOperation.LambdaInvoke:
            {
                var p = (LambdaInvokeParameters)job.Parameters;
                var result = await new LambdaService(factory).InvokeAsync(p.FunctionName, p.Payload, p.InvocationType, cancellationToken);
                return $"Invoked: {p.FunctionName} status={result.StatusCode}";
            }

            case JobOperation.CloudFrontInvalidate:
            {
                var p = (CloudFrontInvalidateParameters)job.Parameters;
                await new CloudFrontService(factory).InvalidateCacheAsync(p.DistributionId, [p.Paths], cancellationToken);
                return $"Invalidated: {p.DistributionId} paths={p.Paths}";
            }

            default:
                throw new InvalidOperationException($"Unsupported operation. operation=[{job.Operation}]");
        }
    }

    // Progress is not used for job execution
    private sealed class NullProgress : IProgress<ProgressUpdate>
    {
        public static NullProgress Instance { get; } = new();

        public void Report(ProgressUpdate value)
        {
        }
    }
}
