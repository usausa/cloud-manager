namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class S3LifecycleDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Inject]
    public required S3Service Service { get; set; }

    private List<S3LifecycleRuleInfo> rules = [];

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            rules = await Service.GetLifecycleAsync(BucketName, CancellationToken);
        });
}
