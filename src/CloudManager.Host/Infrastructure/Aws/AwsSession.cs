namespace CloudManager.Host.Infrastructure.Aws;

using Amazon;
using Amazon.Runtime.CredentialManagement;

using CloudManager.Infrastructure.Aws;

// Holds the current profile name and region per circuit
public sealed class AwsSession
{
    public string ProfileName { get; private set; }

    public RegionEndpoint? Region { get; private set; }

    public IReadOnlyList<string> AvailableProfiles { get; }

    // Whether the current profile exists in ~/.aws; AWS calls fail without it
    public bool IsProfileAvailable => AvailableProfiles.Contains(ProfileName, StringComparer.Ordinal);

    public AwsSession(ILogger<AwsSession> log, AwsSetting setting)
    {
        ProfileName = setting.DefaultProfile;
        if (!String.IsNullOrWhiteSpace(setting.DefaultRegion))
        {
            Region = RegionEndpoint.GetBySystemName(setting.DefaultRegion);
        }

        AvailableProfiles = LoadProfiles(log);
    }

    // Switches the profile and region, throwing when they cannot be resolved
    public void SetProfile(string profileName, string? regionName)
    {
        var (_, region) = CredentialResolver.Resolve(profileName, regionName);
        ProfileName = profileName;
        Region = region;
    }

    private static List<string> LoadProfiles(ILogger<AwsSession> log)
    {
        try
        {
#pragma warning disable IDE0028
            return new CredentialProfileStoreChain().ListProfiles().Select(static x => x.Name).ToList();
#pragma warning restore IDE0028
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Keep the UI working even if the credentials file is broken
            log.WarnAwsProfileLoadFailed(ex);
            return [];
        }
    }
}
