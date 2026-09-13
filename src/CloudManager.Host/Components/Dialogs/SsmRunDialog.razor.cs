namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SsmRunDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string InstanceId { get; set; } = string.Empty;

    private string command = string.Empty;

    private int timeoutSeconds = 60;

    private void Submit() => MudDialog.Close(DialogResult.Ok(new SsmRunParams(command, timeoutSeconds)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record SsmRunParams(string Command, int TimeoutSeconds);
