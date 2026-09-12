namespace CloudManager.Models.Aws.Vpc;

public sealed record VpcNatGwInfo(string NatGatewayId, string Name, string SubnetId, string State, string PublicIp);
