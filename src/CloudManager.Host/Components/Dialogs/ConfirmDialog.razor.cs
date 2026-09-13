namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 危険な操作の確認。強制フラグと、識別子の再入力による確認に対応する
public sealed partial class ConfirmDialog
{
    private bool force;

    private string confirmInput = string.Empty;

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required string Message { get; set; }

    [Parameter]
    public bool ShowForce { get; set; }

    [Parameter]
    public string? RequireConfirmText { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private bool IsSubmitEnabled =>
        (RequireConfirmText is null) || (confirmInput == RequireConfirmText);

    private void OnSubmitClick() => MudDialog.Close(DialogResult.Ok(new ConfirmResult(force)));

    private void OnCancelClick() => MudDialog.Cancel();
}
