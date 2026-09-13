namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class DynamoDbPage
{
    [Inject]
    public required DynamoDbService Service { get; set; }

    private List<DynamoDbTableInfo> tables = [];

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            tables = await Service.ListTablesAsync(CancellationToken);
        });

    private static Color DdbStateColor(string state) => state switch
    {
        "ACTIVE" => Color.Success,
        "CREATING" or "UPDATING" => Color.Warning,
        "DELETING" => Color.Error,
        _ => Color.Default
    };

    private async Task ScanAsync(DynamoDbTableInfo table)
    {
        await DialogService.ShowAsync<DynamoDbScanDialog>("Scan 結果", new DialogParameters<DynamoDbScanDialog>
        {
            { x => x.TableName, table.TableName }
        });
    }

    private async Task ShowTtlAsync(DynamoDbTableInfo table)
    {
        await DialogService.ShowAsync<DynamoDbTtlDialog>("TTL 設定", new DialogParameters<DynamoDbTtlDialog>
        {
            { x => x.TableName, table.TableName }
        });
    }

    private async Task ShowPitrAsync(DynamoDbTableInfo table)
    {
        await DialogService.ShowAsync<DynamoDbPitrDialog>("PITR 設定", new DialogParameters<DynamoDbPitrDialog>
        {
            { x => x.TableName, table.TableName }
        });
    }
}
