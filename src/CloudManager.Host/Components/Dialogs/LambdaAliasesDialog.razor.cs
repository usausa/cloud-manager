namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LambdaAliasesDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    [Inject]
    public required LambdaService Service { get; set; }

    private List<LambdaAliasInfo> aliases = [];

    private bool isDeleting;

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            aliases = await Service.ListAliasesAsync(FunctionName, CancellationToken);
        });

    private async Task DeleteAsync(LambdaAliasInfo alias)
    {
        isDeleting = true;
        try
        {
            await Service.DeleteAliasAsync(FunctionName, alias.Name);
            Snackbar.AddSuccess($"エイリアス {alias.Name} を削除しました");
            aliases = await Service.ListAliasesAsync(FunctionName, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isDeleting = false;
        }
    }
}
