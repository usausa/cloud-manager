namespace CloudManager.Services.Aws;

using System.Globalization;

using Amazon.CloudFront.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.CloudFront;

public sealed class CloudFrontService
{
    private readonly AwsClientFactory factory;

    public CloudFrontService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // CloudFront ディストリビューション一覧を取得する(ページング対応)。
    public async ValueTask<List<CloudFrontDistributionInfo>> ListDistributionsAsync()
    {
        using var cloudFront = factory.CreateCloudFrontClient();
        var result = new List<CloudFrontDistributionInfo>();
        string? marker = null;

        do
        {
            var response = await cloudFront.ListDistributionsAsync(
                new ListDistributionsRequest
                {
                    Marker = marker
                });

            var list = response.DistributionList;

            foreach (var dist in list.Items)
            {
                var origins = string.Join(", ", dist.Origins.Items.Select(o => o.DomainName));
                result.Add(new CloudFrontDistributionInfo(
                    dist.Id,
                    dist.DomainName,
                    origins,
                    dist.Status,
                    dist.LastModifiedTime.GetValueOrDefault()));
            }

            marker = list.IsTruncated.GetValueOrDefault() ? list.NextMarker : null;
        }
        while (marker is not null);

        return result;
    }

    // CloudFront キャッシュを無効化する。
    public async ValueTask InvalidateCacheAsync(string distributionId, IReadOnlyList<string> paths, CancellationToken cancellationToken = default)
    {
        using var cloudFront = factory.CreateCloudFrontClient();
        var request = new CreateInvalidationRequest
        {
            DistributionId = distributionId,
            InvalidationBatch = new InvalidationBatch
            {
                CallerReference = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
                Paths = new Paths
                {
                    Quantity = paths.Count,
                    Items = [.. paths]
                }
            }
        };
        await cloudFront.CreateInvalidationAsync(
            request,
            cancellationToken);
    }
}
