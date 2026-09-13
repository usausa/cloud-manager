namespace CloudManager.Services.Aws;

using Amazon.EC2.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Ebs;

public sealed class EbsService
{
    private readonly AwsClientFactory factory;

    public EbsService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<EbsVolumeInfo>> ListVolumesAsync(string? state = null, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        var filters = new List<Filter>();
        if (!String.IsNullOrEmpty(state))
        {
            filters.Add(new Filter("status", [state]));
        }

        var results = new List<EbsVolumeInfo>();
        string? nextToken = null;
        do
        {
            var response = await ec2.DescribeVolumesAsync(
                new DescribeVolumesRequest
                {
                    Filters = filters.Count > 0 ? filters : null,
                    NextToken = nextToken
                },
                cancellationToken);
            foreach (var v in response.Volumes ?? [])
            {
                var attached = v.Attachments?.FirstOrDefault();
                results.Add(new EbsVolumeInfo(
                    v.VolumeId,
                    v.Size.GetValueOrDefault(),
                    v.VolumeType?.Value ?? string.Empty,
                    v.State?.Value ?? string.Empty,
                    v.AvailabilityZone ?? string.Empty,
                    attached?.InstanceId,
                    v.CreateTime.GetValueOrDefault()));
            }

            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<string> CreateSnapshotAsync(string volumeId, string description, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        var response = await ec2.CreateSnapshotAsync(
            new CreateSnapshotRequest
            {
                VolumeId = volumeId,
                Description = description
            },
            cancellationToken);
        return response.Snapshot.SnapshotId;
    }

    public async ValueTask AttachVolumeAsync(string volumeId, string instanceId, string device, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        await ec2.AttachVolumeAsync(
            new AttachVolumeRequest
            {
                VolumeId = volumeId,
                InstanceId = instanceId,
                Device = device
            },
            cancellationToken);
    }

    public async ValueTask DetachVolumeAsync(string volumeId, bool force, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        await ec2.DetachVolumeAsync(
            new DetachVolumeRequest
            {
                VolumeId = volumeId,
                Force = force
            },
            cancellationToken);
    }
}
