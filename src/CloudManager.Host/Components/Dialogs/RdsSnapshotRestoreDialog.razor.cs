namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class RdsSnapshotRestoreDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string SnapshotId { get; set; } = string.Empty;

    private string newDbInstanceId = string.Empty;

    private string instanceClass = "db.t3.medium";

    private void Submit() => MudDialog.Close(DialogResult.Ok(new RdsSnapshotRestoreParams(newDbInstanceId, instanceClass)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record RdsSnapshotRestoreParams(string NewDbInstanceId, string? InstanceClass);
