namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class DynamoDbTtlDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string TableName { get; set; } = string.Empty;

    [Inject]
    public required DynamoDbService Service { get; set; }

    private DynamoDbTtlInfo? ttlInfo;

    private bool enable;

    private string attributeName = string.Empty;

    private bool isSaving;

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            ttlInfo = await Service.GetTtlAsync(TableName, CancellationToken);
            enable = ttlInfo.Enabled;
            attributeName = ttlInfo.AttributeName ?? string.Empty;
        });

    private async Task SaveAsync()
    {
        isSaving = true;
        try
        {
            var attr = enable ? attributeName : (ttlInfo?.AttributeName ?? "ttl");
            await Service.UpdateTtlAsync(TableName, enable, attr);
            Snackbar.AddSuccess("TTL を更新しました");
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
