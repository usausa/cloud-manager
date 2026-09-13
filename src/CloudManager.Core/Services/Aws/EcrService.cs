namespace CloudManager.Services.Aws;

using Amazon.ECR.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Ecr;

public sealed class EcrService
{
    private readonly AwsClientFactory factory;

    public EcrService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<EcrRepositoryInfo>> ListRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateEcrClient();
        var results = new List<EcrRepositoryInfo>();
        string? nextToken = null;
        do
        {
            var response = await client.DescribeRepositoriesAsync(new DescribeRepositoriesRequest { NextToken = nextToken }, cancellationToken);
            foreach (var r in response.Repositories ?? [])
            {
                results.Add(new EcrRepositoryInfo(
                    r.RepositoryName ?? string.Empty,
                    r.RepositoryUri ?? string.Empty,
                    r.CreatedAt,
                    r.ImageScanningConfiguration?.ScanOnPush ?? false,
                    r.EncryptionConfiguration?.EncryptionType?.Value ?? "AES256"));
            }
            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<List<EcrImageInfo>> ListImagesAsync(string repository, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateEcrClient();
        var results = new List<EcrImageInfo>();
        string? nextToken = null;
        do
        {
            var request = new DescribeImagesRequest
            {
                RepositoryName = repository,
                NextToken = nextToken
            };
            var response = await client.DescribeImagesAsync(request, cancellationToken);
            foreach (var img in response.ImageDetails ?? [])
            {
                var tag = img.ImageTags?.Count > 0 ? img.ImageTags[0] : null;
                results.Add(new EcrImageInfo(
                    tag,
                    img.ImageDigest ?? string.Empty,
                    img.ImagePushedAt,
                    img.ImageSizeInBytes.GetValueOrDefault()));
            }
            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask DeleteImageAsync(string repository, string imageDigest, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateEcrClient();
        await client.BatchDeleteImageAsync(
            new BatchDeleteImageRequest
            {
                RepositoryName = repository,
                ImageIds = [new ImageIdentifier { ImageDigest = imageDigest }]
            },
            cancellationToken);
    }

    public async ValueTask<string?> GetLifecyclePolicyAsync(string repository, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateEcrClient();
        try
        {
            var response = await client.GetLifecyclePolicyAsync(new GetLifecyclePolicyRequest { RepositoryName = repository }, cancellationToken);
            return response.LifecyclePolicyText;
        }
        catch (LifecyclePolicyNotFoundException)
        {
            return null;
        }
    }
}
