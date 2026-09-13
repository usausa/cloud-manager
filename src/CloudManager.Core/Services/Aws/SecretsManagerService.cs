namespace CloudManager.Services.Aws;

using Amazon.SecretsManager.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.SecretsManager;

public sealed class SecretsManagerService
{
    private readonly AwsClientFactory factory;

    public SecretsManagerService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<SecretInfo>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateSecretsManagerClient();
        var results = new List<SecretInfo>();
        string? nextToken = null;
        do
        {
            var response = await client.ListSecretsAsync(new ListSecretsRequest { NextToken = nextToken }, cancellationToken);
            foreach (var s in response.SecretList ?? [])
            {
                results.Add(new SecretInfo(
                    s.Name ?? string.Empty,
                    s.ARN ?? string.Empty,
                    s.LastChangedDate,
                    s.RotationEnabled.GetValueOrDefault()));
            }
            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<SecretValueInfo> GetSecretValueAsync(string id, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateSecretsManagerClient();
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = id }, cancellationToken);
        return new SecretValueInfo(
            response.Name ?? string.Empty,
            response.SecretString ?? string.Empty,
            response.VersionId ?? string.Empty);
    }

    public async ValueTask RotateSecretAsync(string id, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateSecretsManagerClient();
        await client.RotateSecretAsync(
            new RotateSecretRequest
            {
                SecretId = id,
                RotateImmediately = true
            },
            cancellationToken);
    }
}
