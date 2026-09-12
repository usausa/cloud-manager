namespace CloudManager.Models.Aws.Vpc;

public sealed record VpcSubnetInfo(string SubnetId, string Name, string CidrBlock, string AvailabilityZone, bool MapPublicIp, string State);
