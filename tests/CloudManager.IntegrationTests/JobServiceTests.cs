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

    // SQLite への保存と読み出しが往復すること(日時・JSON・列挙の変換を含む)
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
