namespace CloudManager.Host.Infrastructure.Aws;

using Amazon;
using Amazon.Runtime.CredentialManagement;

using CloudManager.Infrastructure.Aws;

// 回線ごとに現在のプロファイル名とリージョンを保持する
public sealed class AwsSession
{
    public string ProfileName { get; private set; }

    public RegionEndpoint? Region { get; private set; }

    public IReadOnlyList<string> AvailableProfiles { get; }

    // 現在のプロファイルが ~/.aws に存在するか。存在しなければAWS呼び出しは資格情報エラーになる
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

    // プロファイルとリージョンを切り替える。解決できない場合は例外
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
            return new CredentialProfileStoreChain().ListProfiles().Select(static x => x.Name).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 資格情報ファイルが壊れていても画面は出す
            log.WarnAwsProfileLoadFailed(ex);
            return [];
        }
    }
}
