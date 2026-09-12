namespace CloudManager.Models.Jobs;

public enum JobOperation
{
    // EC2
    Ec2Start,
    Ec2Stop,
    Ec2Reboot,
    // RDS
    RdsStart,
    RdsStop,
    // ECS
    EcsUpdateDesiredCount,
    // Lambda
    LambdaInvoke,
    // CloudFront
    CloudFrontInvalidate
}
