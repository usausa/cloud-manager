namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class Route53RecordDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private string name = string.Empty;

    private string type = "A";

    private int ttl = 300;

    private string valuesText = string.Empty;

    private void Submit()
    {
        var values = valuesText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        MudDialog.Close(DialogResult.Ok(new Route53RecordParams(name, type, ttl, values)));
    }

    private void Cancel() => MudDialog.Cancel();
}

public sealed record Route53RecordParams(string Name, string Type, int Ttl, IReadOnlyList<string> Values);
