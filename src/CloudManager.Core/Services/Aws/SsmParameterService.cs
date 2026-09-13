namespace CloudManager.Services.Aws;

using Amazon.SimpleSystemsManagement.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.SsmParameter;

public sealed class SsmParameterService
{
    private readonly AwsClientFactory factory;

    public SsmParameterService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<ParameterInfo>> ListParametersAsync(string? pathPrefix = null, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateSsmClient();
        var results = new List<ParameterInfo>();
        string? nextToken = null;
        do
        {
            var request = new DescribeParametersRequest { NextToken = nextToken };
            if (!String.IsNullOrWhiteSpace(pathPrefix))
            {
                request.ParameterFilters =
                [
                    new ParameterStringFilter
                    {
                        Key = "Name",
                        Option = "BeginsWith",
                        Values = [pathPrefix]
                    }
                ];
            }
            var response = await client.DescribeParametersAsync(request, cancellationToken);
            foreach (var p in response.Parameters ?? [])
            {
                results.Add(new ParameterInfo(
                    p.Name ?? string.Empty,
                    p.Type?.Value ?? string.Empty,
                    p.LastModifiedDate,
                    (int)p.Version.GetValueOrDefault()));
            }
            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<ParameterValueInfo> GetParameterAsync(string name, bool withDecryption = false, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateSsmClient();
        var response = await client.GetParameterAsync(new GetParameterRequest
        {
            Name = name,
            WithDecryption = withDecryption
        },
        cancellationToken);
        var p = response.Parameter;
        return new ParameterValueInfo(
            p.Name ?? string.Empty,
            p.Type?.Value ?? string.Empty,
            p.Value ?? string.Empty);
    }

    public async ValueTask PutParameterAsync(
        string name,
        string value,
        string type,
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateSsmClient();
        await client.PutParameterAsync(
            new PutParameterRequest
            {
                Name = name,
                Value = value,
                Type = type,
                Overwrite = overwrite
            },
            cancellationToken);
    }

    public async ValueTask DeleteParameterAsync(string name, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateSsmClient();
        await client.DeleteParameterAsync(new DeleteParameterRequest { Name = name }, cancellationToken);
    }
}
