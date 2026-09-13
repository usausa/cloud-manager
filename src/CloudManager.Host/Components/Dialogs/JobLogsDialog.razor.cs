namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class JobLogsDialog
{
    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required string ErrorDetail { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private void OnCloseClick() => MudDialog.Close();
}
