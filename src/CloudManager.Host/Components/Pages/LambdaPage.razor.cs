namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LambdaPage
{
    [Inject]
    public required LambdaService Service { get; set; }

    private List<LambdaFunctionInfo> functions = [];

    private string searchText = string.Empty;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            functions = await Service.ListFunctionsAsync(CancellationToken);
        });

    private async Task InvokeAsync(LambdaFunctionInfo function)
    {
        var parameters = new DialogParameters<LambdaInvokeDialog>
        {
            { x => x.FunctionName, function.FunctionName }
        };
        var dialog = await DialogService.ShowAsync<LambdaInvokeDialog>("Lambda 実行", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var invokeParams = (LambdaInvokeParams)dialogResult.Data!;

        await RunAsync($"Invoke: {function.FunctionName}", async (_, cancellationToken) =>
        {
            var result = await Service.InvokeAsync(function.FunctionName, invokeParams.Payload, "RequestResponse", cancellationToken);

            var resultParams = new DialogParameters<LambdaResultDialog>
            {
                { x => x.FunctionName, function.FunctionName },
                { x => x.StatusCode, result.StatusCode },
                { x => x.Payload, result.Payload }
            };
            await DialogService.ShowAsync<LambdaResultDialog>("Lambda 実行結果", resultParams);
        });
    }

    private bool FilterFunc(LambdaFunctionInfo f) =>
        String.IsNullOrWhiteSpace(searchText) || f.FunctionName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private async Task ShowEnvVarsAsync(LambdaFunctionInfo fn)
    {
        await DialogService.ShowAsync<LambdaEnvVarsDialog>("環境変数", new DialogParameters<LambdaEnvVarsDialog>
        {
            { x => x.FunctionName, fn.FunctionName }
        });
    }

    private async Task ShowAliasesAsync(LambdaFunctionInfo fn)
    {
        await DialogService.ShowAsync<LambdaAliasesDialog>("エイリアス", new DialogParameters<LambdaAliasesDialog>
        {
            { x => x.FunctionName, fn.FunctionName }
        });
    }

    private async Task ShowConcurrencyAsync(LambdaFunctionInfo fn)
    {
        await DialogService.ShowAsync<LambdaConcurrencyDialog>("同時実行数", new DialogParameters<LambdaConcurrencyDialog>
        {
            { x => x.FunctionName, fn.FunctionName }
        });
    }

    private async Task ShowDlqAsync(LambdaFunctionInfo fn)
    {
        await DialogService.ShowAsync<LambdaDlqDialog>("DLQ 設定", new DialogParameters<LambdaDlqDialog>
        {
            { x => x.FunctionName, fn.FunctionName }
        });
    }
}
