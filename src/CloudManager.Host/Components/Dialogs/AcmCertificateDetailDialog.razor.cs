namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class AcmCertificateDetailDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public AcmCertificateDetail Detail { get; set; } = default!;

    private void Close() => MudDialog.Close();
}
