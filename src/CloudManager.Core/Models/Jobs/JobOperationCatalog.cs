namespace CloudManager.Models.Jobs;

// Selectable operations and display names per service
public static class JobOperationCatalog
{
    public static IReadOnlyList<JobOperation> ForService(JobServiceType type) => type switch
    {
        JobServiceType.Ec2 => [JobOperation.Ec2Start, JobOperation.Ec2Stop, JobOperation.Ec2Reboot],
        JobServiceType.Rds => [JobOperation.RdsStart, JobOperation.RdsStop],
        JobServiceType.Ecs => [JobOperation.EcsUpdateDesiredCount],
        JobServiceType.Lambda => [JobOperation.LambdaInvoke],
        JobServiceType.CloudFront => [JobOperation.CloudFrontInvalidate],
        _ => []
    };

    public static string DisplayName(JobOperation operation) => operation switch
    {
        JobOperation.Ec2Start => "EC2 起動",
        JobOperation.Ec2Stop => "EC2 停止",
        JobOperation.Ec2Reboot => "EC2 再起動",
        JobOperation.RdsStart => "RDS 起動",
        JobOperation.RdsStop => "RDS 停止",
        JobOperation.EcsUpdateDesiredCount => "ECS タスク数更新",
        JobOperation.LambdaInvoke => "Lambda 実行",
        JobOperation.CloudFrontInvalidate => "CloudFront キャッシュ無効化",
        _ => operation.ToString()
    };
}
