namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class EcsDesiredCountDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string ServiceName { get; set; } = string.Empty;

    [Parameter]
    public int CurrentCount { get; set; }

    private int desiredCount;

    protected override void OnInitialized()
    {
        desiredCount = CurrentCount;
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(new EcsDesiredCountParams(desiredCount)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record EcsDesiredCountParams(int DesiredCount);
