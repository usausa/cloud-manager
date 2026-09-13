namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class EcsTasksDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string ClusterName { get; set; } = string.Empty;

    [Parameter]
    public string ServiceName { get; set; } = string.Empty;

    [Inject]
    public required EcsService Service { get; set; }

    private List<EcsTaskInfo> tasks = [];

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            tasks = await Service.ListTasksAsync(ClusterName, ServiceName, CancellationToken);
        });
}
