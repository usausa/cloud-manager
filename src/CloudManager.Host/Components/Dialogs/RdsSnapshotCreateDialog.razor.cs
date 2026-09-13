namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class RdsSnapshotCreateDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string DbInstanceId { get; set; } = string.Empty;

    private string snapshotId = string.Empty;

    protected override void OnInitialized()
    {
        snapshotId = $"{DbInstanceId}-snap-{DateTime.UtcNow:yyyyMMddHHmm}";
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(new RdsSnapshotCreateParams(snapshotId)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record RdsSnapshotCreateParams(string SnapshotId);
