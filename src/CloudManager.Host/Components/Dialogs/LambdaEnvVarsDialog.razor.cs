namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LambdaEnvVarsDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    [Inject]
    public required LambdaService Service { get; set; }

    private List<LambdaEnvVarInfo> envVars = [];

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            envVars = await Service.GetEnvironmentVariablesAsync(FunctionName, CancellationToken);
        });
}
