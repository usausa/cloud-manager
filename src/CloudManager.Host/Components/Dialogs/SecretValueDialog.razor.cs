namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SecretValueDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string SecretName { get; set; } = string.Empty;

    [Parameter]
    public string SecretValue { get; set; } = string.Empty;

    [Parameter]
    public string VersionId { get; set; } = string.Empty;

    private void Close() => MudDialog.Close();
}
