namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SqsMessagesDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string QueueName { get; set; } = string.Empty;

    [Parameter]
    public IReadOnlyList<SqsMessageInfo> Messages { get; set; } = [];

    private void Close() => MudDialog.Close();
}
