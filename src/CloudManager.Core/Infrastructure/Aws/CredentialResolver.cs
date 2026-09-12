namespace CloudManager.Infrastructure.Aws;

using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

// ~/.aws/credentials / ~/.aws/config からプロファイルを解決し AWSCredentials と RegionEndpoint を返す。
public static class CredentialResolver
{
    public static (AWSCredentials Credentials, RegionEndpoint Region) Resolve(string? profileName, string? regionName)
    {
        var chain = new CredentialProfileStoreChain();
        var name = profileName ?? "default";

        if (!chain.TryGetAWSCredentials(name, out var credentials))
        {
            throw new InvalidOperationException($"AWS profile '{name}' not found in ~/.aws/credentials or ~/.aws/config.");
        }

        RegionEndpoint region;
        if (!string.IsNullOrWhiteSpace(regionName))
        {
            region = RegionEndpoint.GetBySystemName(regionName);
        }
        else
        {
            if (!chain.TryGetProfile(name, out var profile) || profile.Region is null)
            {
                throw new InvalidOperationException($"Region not specified. Use --region or set region in ~/.aws/config for profile '{name}'.");
            }

            region = profile.Region;
        }

        return (credentials, region);
    }
}
