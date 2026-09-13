namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// Confirmation for dangerous operations with force flag and identifier re-entry
public sealed partial class ConfirmDialog
{
    private bool force;

    private string confirmInput = string.Empty;

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required string Message { get; set; }

    [Parameter]
    public bool ShowForce { get; set; }

    [Parameter]
    public string? RequireConfirmText { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private bool IsSubmitEnabled =>
        (RequireConfirmText is null) || (confirmInput == RequireConfirmText);

    private void OnSubmitClick() => MudDialog.Close(DialogResult.Ok(new ConfirmResult(force)));

    private void OnCancelClick() => MudDialog.Cancel();
}
