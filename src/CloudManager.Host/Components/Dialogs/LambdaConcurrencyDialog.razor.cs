namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LambdaConcurrencyDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    [Inject]
    public required LambdaService Service { get; set; }

    private LambdaConcurrencyInfo? info;

    private int? newValue;

    private bool isSaving;

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            info = await Service.GetConcurrencyAsync(FunctionName, CancellationToken);
            newValue = info.ReservedConcurrency;
        });

    private async Task SaveAsync()
    {
        isSaving = true;
        try
        {
            await Service.SetReservedConcurrencyAsync(FunctionName, newValue);
            Snackbar.AddSuccess("同時実行数を更新しました");
            MudDialog.Close();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isSaving = false;
        }
    }
}
