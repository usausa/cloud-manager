namespace CloudManager.Models.Aws.Ec2;

public sealed record Ec2InstanceInfo(
    string InstanceId,
    string Name,
    string State,
    string InstanceType,
    string? PublicIp,
    string? PrivateIp,
    DateTime LaunchTime);
