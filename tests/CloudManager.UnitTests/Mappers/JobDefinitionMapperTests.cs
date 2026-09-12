namespace CloudManager.Mappers;

using CloudManager.Models.Entity;
using CloudManager.Models.Jobs;

public sealed class JobDefinitionMapperTests
{
    public static TheoryData<JobParameters> Parameters =>
    [
        new Ec2InstanceParameters("i-0123456789abcdef0"),
        new RdsInstanceParameters("db-1"),
        new EcsDesiredCountParameters("cluster", "service", 2),
        new LambdaInvokeParameters("function", "{\"key\":\"value\"}", "Event"),
        new LambdaInvokeParameters("function", null, "RequestResponse"),
        new CloudFrontInvalidateParameters("E123", "/*")
    ];

    // パラメータはJSONで保存するため、派生型ごとに往復できること
    [Theory]
    [MemberData(nameof(Parameters))]
    public void RoundTripKeepsParameters(JobParameters parameters)
    {
        // Arrange
        var job = new JobDefinition(
            1,
            "job",
            "description",
            "default",
            "ap-northeast-1",
            JobServiceType.Ec2,
            JobOperation.Ec2Start,
            parameters,
            "0 9 * * 1-5",
            JobCronTimeZone.Local,
            true,
            new DateTime(2026, 1, 1, 9, 0, 0),
            new DateTime(2026, 1, 2, 9, 0, 0));

        // Act
        var entity = JobDefinitionMapper.ToEntity(job);
        var restored = JobDefinitionMapper.ToModel(entity);

        // Assert
        Assert.Equal(job, restored);
    }

    [Fact]
    public void ToEntityStoresEnumsAsNames()
    {
        // Arrange
        var job = new JobDefinition(
            0,
            "job",
            null,
            "default",
            "ap-northeast-1",
            JobServiceType.CloudFront,
            JobOperation.CloudFrontInvalidate,
            new CloudFrontInvalidateParameters("E123", "/*"),
            "0 * * * *",
            JobCronTimeZone.Utc,
            false,
            default,
            default);

        // Act
        var entity = JobDefinitionMapper.ToEntity(job);

        // Assert
        Assert.Equal("CloudFront", entity.ServiceType);
        Assert.Equal("CloudFrontInvalidate", entity.Operation);
        Assert.Equal("Utc", entity.CronTimeZone);
        Assert.Contains("\"$kind\":\"CloudFrontInvalidateParameters\"", entity.ParametersJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ToModelThrowsForBrokenParameters()
    {
        // Arrange
        var entity = new JobDefinitionEntity
        {
            Id = 1,
            Name = "job",
            ProfileName = "default",
            RegionName = "ap-northeast-1",
            ServiceType = "Ec2",
            Operation = "Ec2Start",
            ParametersJson = "null",
            CronExpression = "0 * * * *",
            CronTimeZone = "Utc"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => { _ = JobDefinitionMapper.ToModel(entity); });
    }
}
