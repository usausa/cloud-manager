namespace CloudManager.Models.Aws.Vpc;

public sealed record VpcDetail(
    VpcInfo Vpc,
    IReadOnlyList<VpcSubnetInfo> Subnets,
    IReadOnlyList<VpcRouteTableInfo> RouteTables,
    IReadOnlyList<VpcSecurityGroupInfo> SecurityGroups,
    IReadOnlyList<VpcIgwInfo> InternetGateways,
    IReadOnlyList<VpcNatGwInfo> NatGateways);
