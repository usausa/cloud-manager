namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ApiGatewayDeployDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string StageName { get; set; } = string.Empty;

    private string? description;

    private void Submit() => MudDialog.Close(DialogResult.Ok(description));

    private void Cancel() => MudDialog.Cancel();
}
