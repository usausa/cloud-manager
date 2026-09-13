namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LambdaInvokeDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    private string payload = "{}";

    private void Submit() => MudDialog.Close(DialogResult.Ok(new LambdaInvokeParams(payload)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record LambdaInvokeParams(string Payload);
