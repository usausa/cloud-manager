namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Infrastructure.Aws;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

public sealed partial class SettingsPage
{
    private static readonly string[] Regions =
    [
        "us-east-1", "us-east-2", "us-west-1", "us-west-2",
        "ap-northeast-1", "ap-northeast-2", "ap-northeast-3",
        "ap-southeast-1", "ap-southeast-2", "ap-south-1",
        "eu-west-1", "eu-west-2", "eu-central-1",
        "ca-central-1", "sa-east-1"
    ];

    private string selectedProfile = string.Empty;

    private string selectedRegion = string.Empty;

    [Inject]
    public required AwsSession Session { get; set; }

    protected override void OnInitialized()
    {
        selectedProfile = Session.ProfileName;
        selectedRegion = Session.Region?.SystemName ?? string.Empty;
    }

    private void Apply()
    {
        ErrorMessage = null;
        try
        {
            Session.SetProfile(selectedProfile, selectedRegion);
            Snackbar.AddSuccess("設定を適用しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
    }
}
