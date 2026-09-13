namespace CloudManager.Services.Aws;

using Amazon.APIGateway.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.ApiGateway;

public sealed class ApiGatewayService
{
    private readonly AwsClientFactory factory;

    public ApiGatewayService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<ApiGatewayInfo>> ListRestApisAsync(CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateApiGatewayClient();
        var results = new List<ApiGatewayInfo>();
        string? position = null;
        do
        {
            var response = await client.GetRestApisAsync(new GetRestApisRequest { Position = position }, cancellationToken);
            foreach (var api in response.Items ?? [])
            {
                results.Add(new ApiGatewayInfo(
                    api.Id ?? string.Empty,
                    api.Name ?? string.Empty,
                    api.Description,
                    api.CreatedDate));
            }
            position = response.Position;
        }
        while (!String.IsNullOrEmpty(position));
        return results;
    }

    public async ValueTask<List<StageInfo>> ListStagesAsync(string restApiId, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateApiGatewayClient();
        var response = await client.GetStagesAsync(new GetStagesRequest { RestApiId = restApiId }, cancellationToken);
#pragma warning disable IDE0028
        return (response.Item ?? []).Select(s => new StageInfo(
            s.StageName ?? string.Empty,
            s.DeploymentId,
            s.LastUpdatedDate,
            s.TracingEnabled.GetValueOrDefault())).ToList();
#pragma warning restore IDE0028
    }

    public async ValueTask<string> CreateDeploymentAsync(
        string restApiId,
        string stageName,
        string? description,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateApiGatewayClient();
        var response = await client.CreateDeploymentAsync(
            new CreateDeploymentRequest
            {
                RestApiId = restApiId,
                StageName = stageName,
                Description = description
            },
            cancellationToken);
        return response.Id ?? string.Empty;
    }
}
