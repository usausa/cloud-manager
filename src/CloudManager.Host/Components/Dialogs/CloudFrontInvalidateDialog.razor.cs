namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class CloudFrontInvalidateDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string DistributionId { get; set; } = string.Empty;

    private string paths = "/*";

    private void Submit() => MudDialog.Close(DialogResult.Ok(new CloudFrontInvalidateParams(paths)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record CloudFrontInvalidateParams(string Paths);
