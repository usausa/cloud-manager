namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LambdaDlqDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    [Inject]
    public required LambdaService Service { get; set; }

    private LambdaDlqInfo? dlqInfo;

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            dlqInfo = await Service.GetDlqAsync(FunctionName, CancellationToken);
        });
}
