namespace CloudManager.Host.Components.Pages;

using Amazon;

using CloudManager.Host.Infrastructure.Aws;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

public sealed partial class SettingsPage
{
#pragma warning disable IDE0028
    private static readonly IReadOnlyList<string> Regions = RegionEndpoint.EnumerableAllRegions
        .Where(static x => x.PartitionName == "aws")
        .Select(static x => x.SystemName)
        .Order(StringComparer.Ordinal)
        .ToList();
#pragma warning restore IDE0028

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
