namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class RdsEventsDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string SourceIdentifier { get; set; } = string.Empty;

    [Inject]
    public required RdsEventService Service { get; set; }

    private List<RdsEventInfo> events = [];

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            // Last 7 days
            var end = DateTime.UtcNow;
            var start = end.AddDays(-7);
            events = await Service.ListEventsAsync(SourceIdentifier, "db-instance", start, end, CancellationToken);
        });
}
