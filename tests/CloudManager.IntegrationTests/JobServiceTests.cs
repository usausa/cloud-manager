namespace CloudManager;

using CloudManager.Models.Jobs;
using CloudManager.Services;

using Microsoft.Extensions.DependencyInjection;

public sealed class JobServiceTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public JobServiceTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    private static JobDefinition CreateJob(string name) => new(
        0,
        name,
        "description",
        "default",
        "ap-northeast-1",
        JobServiceType.Ec2,
        JobOperation.Ec2Stop,
        new Ec2InstanceParameters("i-0123456789abcdef0"),
        "0 21 * * 1-5",
        JobCronTimeZone.Local,
        false,
        default,
        default);

    public static TheoryData<JobServiceType, JobOperation, JobParameters> Parameters =>
    [
        (JobServiceType.Ec2, JobOperation.Ec2Start, new Ec2InstanceParameters("i-0123456789abcdef0")),
        (JobServiceType.Rds, JobOperation.RdsStop, new RdsInstanceParameters("db-1")),
        (JobServiceType.Ecs, JobOperation.EcsUpdateDesiredCount, new EcsDesiredCountParameters("cluster", "service", 2)),
        (JobServiceType.Lambda, JobOperation.LambdaInvoke, new LambdaInvokeParameters("function", "{\"key\":\"value\"}", "Event")),
        (JobServiceType.Lambda, JobOperation.LambdaInvoke, new LambdaInvokeParameters("function", null, "RequestResponse")),
        (JobServiceType.CloudFront, JobOperation.CloudFrontInvalidate, new CloudFrontInvalidateParameters("E123", "/*"))
    ];

    // Operation parameters round-trip through JSON
    [Theory]
    [MemberData(nameof(Parameters))]
    public async Task ParametersRoundTrip(JobServiceType serviceType, JobOperation operation, JobParameters parameters)
    {
        // Arrange
        _ = factory.CreateClient();
        var service = factory.Services.GetRequiredService<JobService>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var job = CreateJob("parameters") with { ServiceType = serviceType, Operation = operation, Parameters = parameters };

        // Act
        var id = await service.InsertAsync(job, cancellationToken);
        var restored = await service.QueryAsync(id, cancellationToken);
        await service.DeleteAsync(id, cancellationToken);

        // Assert
        Assert.NotNull(restored);
        Assert.Equal(serviceType, restored.ServiceType);
        Assert.Equal(operation, restored.Operation);
        Assert.Equal(parameters, restored.Parameters);
    }

    // Definitions round-trip through SQLite including date, JSON and enum conversion
    [Fact]
    public async Task JobDefinitionRoundTrip()
    {
        // Arrange
        _ = factory.CreateClient();
        var service = factory.Services.GetRequiredService<JobService>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var id = await service.InsertAsync(CreateJob("round-trip"), cancellationToken);
        var inserted = await service.QueryAsync(id, cancellationToken);
        var updated = await service.UpdateAsync(inserted! with { Name = "updated", IsEnabled = true }, cancellationToken);
        var queried = await service.QueryAsync(id, cancellationToken);
        var all = await service.QueryAllAsync(cancellationToken);
        var deleted = await service.DeleteAsync(id, cancellationToken);
        var afterDelete = await service.QueryAsync(id, cancellationToken);

        // Assert
        Assert.NotNull(inserted);
        Assert.Equal("round-trip", inserted.Name);
        Assert.Equal(new Ec2InstanceParameters("i-0123456789abcdef0"), inserted.Parameters);
        Assert.Equal(JobCronTimeZone.Local, inserted.CronTimeZone);
        Assert.NotEqual(default, inserted.CreatedAt);
        Assert.True(updated);
        Assert.Equal("updated", queried!.Name);
        Assert.True(queried.IsEnabled);
        Assert.True(queried.UpdatedAt >= inserted.UpdatedAt);
        Assert.Contains(all, static x => x.Name == "updated");
        Assert.True(deleted);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task JobLogTrimKeepsLatestEntries()
    {
        // Arrange
        _ = factory.CreateClient();
        var jobService = factory.Services.GetRequiredService<JobService>();
        var logService = factory.Services.GetRequiredService<JobLogService>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var jobId = await jobService.InsertAsync(CreateJob("log"), cancellationToken);

        // Act
        for (var i = 0; i < 5; i++)
        {
            var logId = await logService.StartAsync(jobId, "log", cancellationToken);
            await logService.FinishAsync(logId, JobExecutionStatus.Success, $"message-{i}", null, cancellationToken);
        }

        await logService.TrimAsync(jobId, 3, cancellationToken);
        var logs = await logService.QueryByJobAsync(jobId, 10, cancellationToken);
        var recent = await logService.QueryRecentAsync(1, cancellationToken);

        // Assert
        Assert.Equal(3, logs.Count);
        Assert.All(logs, static x => Assert.Equal(JobExecutionStatus.Success, x.Status));
        Assert.All(logs, static x => Assert.NotNull(x.FinishedAt));
        Assert.Equal("message-4", logs[0].Message);
        Assert.Single(recent);

        await jobService.DeleteAsync(jobId, cancellationToken);
    }
}
