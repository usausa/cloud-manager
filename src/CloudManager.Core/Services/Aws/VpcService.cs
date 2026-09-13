namespace CloudManager.Services.Aws;

using Amazon.EC2.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Vpc;

public sealed class VpcService
{
    private readonly AwsClientFactory factory;

    public VpcService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists VPCs
    public async ValueTask<List<VpcInfo>> ListVpcsAsync(CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        var response = await ec2.DescribeVpcsAsync(
            new DescribeVpcsRequest(),
            cancellationToken);
#pragma warning disable IDE0028
        return (response.Vpcs ?? [])
            .Select(v => new VpcInfo(
                v.VpcId,
                v.Tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty,
                v.CidrBlock,
                v.IsDefault.GetValueOrDefault(),
                v.State.Value))
            .ToList();
#pragma warning restore IDE0028
    }

    // Gets the details of a VPC
    public async ValueTask<VpcDetail> GetVpcDetailAsync(string vpcId, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        var vpcResponse = await ec2.DescribeVpcsAsync(
            new DescribeVpcsRequest { VpcIds = [vpcId] },
            cancellationToken);
        var vpc = (vpcResponse.Vpcs ?? []).First();
        var vpcInfo = new VpcInfo(
            vpc.VpcId,
            vpc.Tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty,
            vpc.CidrBlock,
            vpc.IsDefault.GetValueOrDefault(),
            vpc.State.Value);

        var vpcFilter = new Filter("vpc-id", [vpcId]);

        var subnetResponse = await ec2.DescribeSubnetsAsync(
            new DescribeSubnetsRequest { Filters = [vpcFilter] },
            cancellationToken);
        var subnets = (subnetResponse.Subnets ?? [])
            .Select(s => new VpcSubnetInfo(
                s.SubnetId,
                s.Tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty,
                s.CidrBlock,
                s.AvailabilityZone,
                s.MapPublicIpOnLaunch.GetValueOrDefault(),
                s.State.Value))
            .ToList();

        var rtResponse = await ec2.DescribeRouteTablesAsync(
            new DescribeRouteTablesRequest { Filters = [vpcFilter] },
            cancellationToken);
        var routeTables = (rtResponse.RouteTables ?? [])
            .Select(rt => new VpcRouteTableInfo(
                rt.RouteTableId,
                rt.Tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty,
                (rt.Associations ?? []).Any(a => a.Main.GetValueOrDefault()),
                rt.Routes?.Count ?? 0))
            .ToList();

        var sgResponse = await ec2.DescribeSecurityGroupsAsync(
            new DescribeSecurityGroupsRequest { Filters = [vpcFilter] },
            cancellationToken);
        var securityGroups = (sgResponse.SecurityGroups ?? [])
            .Select(sg => new VpcSecurityGroupInfo(sg.GroupId, sg.GroupName, sg.Description))
            .ToList();

        var igwResponse = await ec2.DescribeInternetGatewaysAsync(
            new DescribeInternetGatewaysRequest
            {
                Filters = [new Filter("attachment.vpc-id", [vpcId])]
            },
            cancellationToken);
        var igws = (igwResponse.InternetGateways ?? [])
            .Select(igw => new VpcIgwInfo(
                igw.InternetGatewayId,
                igw.Tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty,
                igw.Attachments?.FirstOrDefault()?.State?.Value ?? "-"))
            .ToList();

        var natResponse = await ec2.DescribeNatGatewaysAsync(
            new DescribeNatGatewaysRequest
            {
                Filter = [vpcFilter]
            },
            cancellationToken);
        var natGws = (natResponse.NatGateways ?? [])
            .Select(nat => new VpcNatGwInfo(
                nat.NatGatewayId,
                nat.Tags?.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty,
                nat.SubnetId ?? string.Empty,
                nat.State?.Value ?? string.Empty,
                nat.NatGatewayAddresses?.FirstOrDefault()?.PublicIp ?? "-"))
            .ToList();

        return new VpcDetail(vpcInfo, subnets, routeTables, securityGroups, igws, natGws);
    }
}
