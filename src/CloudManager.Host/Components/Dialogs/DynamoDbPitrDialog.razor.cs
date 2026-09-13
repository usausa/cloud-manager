namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class DynamoDbPitrDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string TableName { get; set; } = string.Empty;

    [Inject]
    public required DynamoDbService Service { get; set; }

    private DynamoDbPitrInfo? pitrInfo;

    private bool enable;

    private bool isSaving;

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            pitrInfo = await Service.GetPitrAsync(TableName, CancellationToken);
            enable = pitrInfo.Enabled;
        });

    private async Task SaveAsync()
    {
        isSaving = true;
        try
        {
            await Service.UpdatePitrAsync(TableName, enable);
            Snackbar.AddSuccess("PITR を更新しました");
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
