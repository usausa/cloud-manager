namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SnsPublishDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string TopicArn { get; set; } = string.Empty;

    private string? subject;

    private string message = string.Empty;

    private void Submit() => MudDialog.Close(DialogResult.Ok(new SnsPublishParams(subject, message)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record SnsPublishParams(string? Subject, string Message);
