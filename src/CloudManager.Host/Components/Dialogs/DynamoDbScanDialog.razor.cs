namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class DynamoDbScanDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string TableName { get; set; } = string.Empty;

    [Inject]
    public required DynamoDbService Service { get; set; }

    private List<DynamoDbItemInfo> items = [];

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            items = await Service.ScanAsync(TableName);
        });
}
