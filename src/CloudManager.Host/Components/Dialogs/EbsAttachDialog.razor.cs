namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed record EbsAttachParams(string InstanceId, string Device);

public sealed partial class EbsAttachDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string VolumeId { get; set; } = string.Empty;

    private string instanceId = string.Empty;

    private string device = string.Empty;

    private bool IsValid => !String.IsNullOrWhiteSpace(instanceId) && !String.IsNullOrWhiteSpace(device);

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(new EbsAttachParams(instanceId, device)));
}
