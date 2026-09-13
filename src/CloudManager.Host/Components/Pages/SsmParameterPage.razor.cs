namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SsmParameterPage
{
    [Inject]
    public required SsmParameterService Service { get; set; }

    private List<ParameterInfo> parameters = [];

    private string searchText = string.Empty;

    private string? pathPrefix;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            parameters = await Service.ListParametersAsync(pathPrefix, CancellationToken);
        });

    private bool FilterFunc(ParameterInfo p) =>
        String.IsNullOrWhiteSpace(searchText) ||
        p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        p.Type.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private Task ShowValueAsync(ParameterInfo param) =>
        RunAsync("取得中...", async (_, _) =>
        {
            var isSecure = param.Type == "SecureString";
            var result = await Service.GetParameterAsync(param.Name, isSecure, CancellationToken);
            var dialogParameters = new DialogParameters<SsmParameterValueDialog>
            {
                { x => x.ParameterName, result.Name },
                { x => x.ParameterValue, result.Value },
                { x => x.ParameterType, result.Type }
            };
            await DialogService.ShowAsync<SsmParameterValueDialog>("パラメータ値", dialogParameters);
        });

    private async Task OpenAddDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<SsmParameterEditDialog>("パラメータ追加");
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        var p = (SsmParameterEditParams)result.Data!;
        await SaveParameterAsync(p);
    }

    private async Task OpenEditDialogAsync(ParameterInfo param)
    {
        ParameterValueInfo? current = null;
        await RunAsync("取得中...", async (_, _) =>
        {
            current = await Service.GetParameterAsync(param.Name, param.Type == "SecureString", CancellationToken);
        });
        if (current is null)
        {
            return;
        }

        var dialogParams = new DialogParameters<SsmParameterEditDialog>
        {
            { x => x.InitialName, current.Name },
            { x => x.InitialValue, current.Value },
            { x => x.InitialType, current.Type },
            { x => x.IsEdit, true }
        };
        var dialog = await DialogService.ShowAsync<SsmParameterEditDialog>("パラメータ編集", dialogParams);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }
        var editedParams = (SsmParameterEditParams)dialogResult.Data!;
        await SaveParameterAsync(editedParams);
    }

    private Task SaveParameterAsync(SsmParameterEditParams p) =>
        RunAsync("保存中...", async (_, cancellationToken) =>
        {
            await Service.PutParameterAsync(p.Name, p.Value, p.Type, p.Overwrite, cancellationToken);
            Snackbar.AddSuccess($"パラメータ保存完了: {p.Name}");
            await LoadAsync();
        });

    private async Task DeleteAsync(ParameterInfo param)
    {
        var dialogParams = new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "パラメータ削除確認" },
            { x => x.Message, $"パラメータ「{param.Name}」を削除します。" },
            { x => x.RequireConfirmText, param.Name }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("削除確認", dialogParams);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteParameterAsync(param.Name, cancellationToken);
            Snackbar.AddSuccess($"パラメータ削除完了: {param.Name}");
            await LoadAsync();
        });
    }
}
