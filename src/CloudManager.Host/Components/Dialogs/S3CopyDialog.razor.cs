namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed record S3CopyParams(string DstBucket, string DstKey, bool Move);

public sealed partial class S3CopyDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string SrcBucket { get; set; } = string.Empty;

    [Parameter]
    public string SrcKey { get; set; } = string.Empty;

    private string dstBucket = string.Empty;

    private string dstKey = string.Empty;

    private bool move;

    private bool IsValid => !String.IsNullOrWhiteSpace(dstBucket) && !String.IsNullOrWhiteSpace(dstKey);

    protected override void OnParametersSet()
    {
        dstBucket = SrcBucket;
        dstKey = SrcKey;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(new S3CopyParams(dstBucket, dstKey, move)));
}
