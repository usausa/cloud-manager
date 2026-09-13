namespace CloudManager.Host.Mappers;

using CloudManager.Host.Models.Forms;

// JobDefinition と編集フォームの相互変換。操作ごとのパラメータはフォームの該当項目に展開する
public static class JobMapper
{
    public static JobForm ToForm(JobDefinition job)
    {
        var form = new JobForm
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            ProfileName = job.ProfileName,
            RegionName = job.RegionName,
            ServiceType = job.ServiceType,
            Operation = job.Operation,
            CronExpression = job.CronExpression,
            CronTimeZone = job.CronTimeZone,
            IsEnabled = job.IsEnabled,
            CreatedAt = job.CreatedAt
        };

        switch (job.Parameters)
        {
            case Ec2InstanceParameters p:
                form.InstanceId = p.InstanceId;
                break;
            case RdsInstanceParameters p:
                form.DbInstanceId = p.DbInstanceId;
                break;
            case EcsDesiredCountParameters p:
                form.Cluster = p.Cluster;
                form.ServiceName = p.ServiceName;
                form.DesiredCount = p.DesiredCount;
                break;
            case LambdaInvokeParameters p:
                form.FunctionName = p.FunctionName;
                form.Payload = p.Payload;
                form.InvocationType = p.InvocationType;
                break;
            case CloudFrontInvalidateParameters p:
                form.DistributionId = p.DistributionId;
                form.Paths = p.Paths;
                break;
        }

        return form;
    }

    public static JobDefinition ToDefinition(JobForm form) => new(
        form.Id,
        form.Name,
        String.IsNullOrWhiteSpace(form.Description) ? null : form.Description,
        form.ProfileName,
        form.RegionName,
        form.ServiceType,
        form.Operation,
        ToParameters(form),
        form.CronExpression,
        form.CronTimeZone,
        form.IsEnabled,
        form.CreatedAt,
        form.CreatedAt);

    private static JobParameters ToParameters(JobForm form) => form.Operation switch
    {
        JobOperation.Ec2Start or JobOperation.Ec2Stop or JobOperation.Ec2Reboot => new Ec2InstanceParameters(form.InstanceId),
        JobOperation.RdsStart or JobOperation.RdsStop => new RdsInstanceParameters(form.DbInstanceId),
        JobOperation.EcsUpdateDesiredCount => new EcsDesiredCountParameters(form.Cluster, form.ServiceName, form.DesiredCount),
        JobOperation.LambdaInvoke => new LambdaInvokeParameters(form.FunctionName, String.IsNullOrWhiteSpace(form.Payload) ? null : form.Payload, form.InvocationType),
        JobOperation.CloudFrontInvalidate => new CloudFrontInvalidateParameters(form.DistributionId, form.Paths),
        _ => throw new InvalidOperationException($"Unsupported operation. operation=[{form.Operation}]")
    };
}
