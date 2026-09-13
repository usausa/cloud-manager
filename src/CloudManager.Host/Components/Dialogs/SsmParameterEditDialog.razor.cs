namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SsmParameterEditDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string? InitialName { get; set; }

    [Parameter]
    public string? InitialValue { get; set; }

    [Parameter]
    public string? InitialType { get; set; }

    [Parameter]
    public bool IsEdit { get; set; }

    private string name = string.Empty;

    private string value = string.Empty;

    private string type = "String";

    private bool overwrite = true;

    protected override void OnParametersSet()
    {
        name = InitialName ?? string.Empty;
        value = InitialValue ?? string.Empty;
        type = InitialType ?? "String";
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(new SsmParameterEditParams(name, value, type, overwrite)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record SsmParameterEditParams(string Name, string Value, string Type, bool Overwrite);
