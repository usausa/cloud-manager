namespace CloudManager.Services.Aws;

using Amazon.EC2.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.ElasticIp;

public sealed class ElasticIpService
{
    private readonly AwsClientFactory factory;

    public ElasticIpService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<ElasticIpInfo>> ListElasticIpsAsync(CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        var response = await ec2.DescribeAddressesAsync(new DescribeAddressesRequest(), cancellationToken);
#pragma warning disable IDE0028
        return (response.Addresses ?? [])
            .Select(a => new ElasticIpInfo(
                a.AllocationId ?? string.Empty,
                a.PublicIp ?? string.Empty,
                a.AssociationId,
                a.InstanceId,
                a.NetworkInterfaceId))
            .ToList();
#pragma warning restore IDE0028
    }

    public async ValueTask AssociateElasticIpAsync(string allocationId, string instanceId, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        await ec2.AssociateAddressAsync(
            new AssociateAddressRequest
            {
                AllocationId = allocationId,
                InstanceId = instanceId
            },
            cancellationToken);
    }

    public async ValueTask DisassociateElasticIpAsync(string associationId, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        await ec2.DisassociateAddressAsync(
            new DisassociateAddressRequest
            {
                AssociationId = associationId
            },
            cancellationToken);
    }
}
