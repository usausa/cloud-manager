namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using MudBlazor;

public sealed partial class S3UploadDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    private IBrowserFile? selectedFile;

    private string key = string.Empty;

    private void OnFileChanged(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        if (String.IsNullOrWhiteSpace(key))
        {
            key = e.File.Name;
        }
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(new S3UploadParams(selectedFile!, key)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record S3UploadParams(IBrowserFile File, string Key);
