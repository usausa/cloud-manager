namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ApiGatewayPage
{
    [Inject]
    public required ApiGatewayService Service { get; set; }

    private List<ApiGatewayInfo> apis = [];

    private List<StageInfo> stages = [];

    private ApiGatewayInfo? selectedApi;

    private bool isStagesLoading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedApi = null;
        stages = [];
        return LoadAsync(async () =>
        {
            apis = await Service.ListRestApisAsync(CancellationToken);
        });
    }

    private Task OnApiSelectedAsync(ApiGatewayInfo? api)
    {
        selectedApi = api;
        stages = [];
        if (api is not null)
        {
            return LoadStagesAsync();
        }
        return Task.CompletedTask;
    }

    private async Task LoadStagesAsync()
    {
        if (selectedApi is null)
        {
            return;
        }
        isStagesLoading = true;
        try
        {
            stages = await Service.ListStagesAsync(selectedApi.Id, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isStagesLoading = false;
        }
    }

    private async Task DeployAsync(StageInfo stage)
    {
        if (selectedApi is null)
        {
            return;
        }
        var dialogParams = new DialogParameters<ApiGatewayDeployDialog>
        {
            { x => x.StageName, stage.StageName }
        };
        var dialog = await DialogService.ShowAsync<ApiGatewayDeployDialog>("デプロイ", dialogParams);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        var description = (string?)result.Data;

        await RunAsync("デプロイ中...", async (_, cancellationToken) =>
        {
            var deploymentId = await Service.CreateDeploymentAsync(
                selectedApi.Id, stage.StageName, description, cancellationToken);
            Snackbar.AddSuccess($"デプロイ完了: {stage.StageName} (ID: {deploymentId})");
            await LoadStagesAsync();
        });
    }
}
