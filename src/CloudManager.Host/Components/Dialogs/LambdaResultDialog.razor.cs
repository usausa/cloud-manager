namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LambdaResultDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    [Parameter]
    public int StatusCode { get; set; }

    [Parameter]
    public string Payload { get; set; } = string.Empty;

    private void Close() => MudDialog.Close();
}
