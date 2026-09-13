namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SsmParameterValueDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string ParameterName { get; set; } = string.Empty;

    [Parameter]
    public string ParameterValue { get; set; } = string.Empty;

    [Parameter]
    public string ParameterType { get; set; } = string.Empty;

    private void Close() => MudDialog.Close();
}
