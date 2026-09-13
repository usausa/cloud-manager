namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class CloudWatchPage
{
    [Inject]
    public required CloudWatchService Service { get; set; }

    private List<CloudWatchAlarmInfo> alarms = [];

    private string searchText = string.Empty;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            alarms = await Service.ListAlarmsAsync();
        });

    private bool FilterFunc(CloudWatchAlarmInfo a) =>
        String.IsNullOrWhiteSpace(searchText) || a.AlarmName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private static Color AlarmStateColor(string state) => state switch
    {
        "OK" => Color.Success,
        "ALARM" => Color.Error,
        "INSUFFICIENT_DATA" => Color.Warning,
        _ => Color.Default
    };
}
