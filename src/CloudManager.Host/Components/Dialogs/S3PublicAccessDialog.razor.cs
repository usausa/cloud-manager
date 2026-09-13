namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class S3PublicAccessDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Inject]
    public required S3Service Service { get; set; }

    private S3PublicAccessReport? report;

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            report = await Service.GetBucketPublicAccessAsync(BucketName, CancellationToken);
        });
}
