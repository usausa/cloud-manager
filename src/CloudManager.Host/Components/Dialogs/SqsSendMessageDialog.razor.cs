namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SqsSendMessageDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string QueueName { get; set; } = string.Empty;

    private string body = string.Empty;

    private void Submit() => MudDialog.Close(DialogResult.Ok(new SqsSendMessageParams(body)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record SqsSendMessageParams(string Body);
