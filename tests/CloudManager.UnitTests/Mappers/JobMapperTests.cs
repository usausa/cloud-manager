namespace CloudManager.Mappers;

using CloudManager.Host.Mappers;
using CloudManager.Models.Jobs;

public sealed class JobMapperTests
{
    public static TheoryData<JobServiceType, JobOperation, JobParameters> Cases =>
    [
        (JobServiceType.Ec2, JobOperation.Ec2Reboot, new Ec2InstanceParameters("i-1")),
        (JobServiceType.Rds, JobOperation.RdsStop, new RdsInstanceParameters("db-1")),
        (JobServiceType.Ecs, JobOperation.EcsUpdateDesiredCount, new EcsDesiredCountParameters("cluster", "service", 3)),
        (JobServiceType.Lambda, JobOperation.LambdaInvoke, new LambdaInvokeParameters("function", "{}", "RequestResponse")),
        (JobServiceType.CloudFront, JobOperation.CloudFrontInvalidate, new CloudFrontInvalidateParameters("E1", "/*"))
    ];

    // フォームへ展開して戻しても定義が変わらないこと
    [Theory]
    [MemberData(nameof(Cases))]
    public void RoundTripKeepsDefinition(JobServiceType serviceType, JobOperation operation, JobParameters parameters)
    {
        var job = new JobDefinition(
            5,
            "job",
            "description",
            "default",
            "ap-northeast-1",
            serviceType,
            operation,
            parameters,
            "0 9 * * *",
            JobCronTimeZone.Utc,
            false,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 1));

        var restored = JobMapper.ToDefinition(JobMapper.ToForm(job));

        Assert.Equal(job, restored);
    }

    [Fact]
    public void BlankDescriptionAndPayloadBecomeNull()
    {
        var form = JobMapper.ToForm(new JobDefinition(0, "job", null, "default", "ap-northeast-1", JobServiceType.Lambda, JobOperation.LambdaInvoke, new LambdaInvokeParameters("function", null, "Event"), "0 * * * *", JobCronTimeZone.Local, true, default, default));
        form.Description = " ";
        form.Payload = string.Empty;

        var job = JobMapper.ToDefinition(form);

        Assert.Null(job.Description);
        Assert.Null(((LambdaInvokeParameters)job.Parameters).Payload);
    }
}
