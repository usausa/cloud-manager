namespace CloudManager.Models.Forms;

using CloudManager.Host.Models.Forms;
using CloudManager.Models.Jobs;

public sealed class JobFormValidatorTests
{
    private static readonly JobFormValidator Validator = new();

    private static JobForm CreateValidForm() => new()
    {
        Name = "job",
        ProfileName = "default",
        RegionName = "ap-northeast-1",
        ServiceType = JobServiceType.Ec2,
        Operation = JobOperation.Ec2Start,
        InstanceId = "i-0123456789abcdef0",
        CronExpression = "0 9 * * 1-5"
    };

    [Fact]
    public void ValidFormPasses()
    {
        var result = Validator.Validate(CreateValidForm());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("0 25 * * *")]
    public void InvalidCronFails(string cron)
    {
        var form = CreateValidForm();
        form.CronExpression = cron;

        var result = Validator.Validate(form);

        Assert.Contains(result.Errors, static x => x.PropertyName == nameof(JobForm.CronExpression));
    }

    // Only the fields required by the operation are validated
    [Fact]
    public void OperationSpecificParametersAreRequired()
    {
        var form = CreateValidForm();
        form.ServiceType = JobServiceType.Ecs;
        form.Operation = JobOperation.EcsUpdateDesiredCount;
        form.InstanceId = string.Empty;

        var result = Validator.Validate(form);

        Assert.Contains(result.Errors, static x => x.PropertyName == nameof(JobForm.Cluster));
        Assert.Contains(result.Errors, static x => x.PropertyName == nameof(JobForm.ServiceName));
        Assert.DoesNotContain(result.Errors, static x => x.PropertyName == nameof(JobForm.InstanceId));
    }

    [Fact]
    public void OperationMustBelongToService()
    {
        var form = CreateValidForm();
        form.ServiceType = JobServiceType.Rds;

        var result = Validator.Validate(form);

        Assert.Contains(result.Errors, static x => x.PropertyName == nameof(JobForm.Operation));
    }
}
