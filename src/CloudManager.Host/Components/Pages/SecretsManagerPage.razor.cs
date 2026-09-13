namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SecretsManagerPage
{
    [Inject]
    public required SecretsManagerService Service { get; set; }

    private List<SecretInfo> secrets = [];

    private string searchText = string.Empty;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            secrets = await Service.ListSecretsAsync(CancellationToken);
        });

    private bool FilterFunc(SecretInfo s) =>
        String.IsNullOrWhiteSpace(searchText) ||
        s.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        s.Arn.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private Task ShowValueAsync(SecretInfo secret) =>
        RunAsync("取得中...", async (_, _) =>
        {
            var value = await Service.GetSecretValueAsync(secret.Arn, CancellationToken);
            var parameters = new DialogParameters<SecretValueDialog>
            {
                { x => x.SecretName, value.Name },
                { x => x.SecretValue, value.SecretString },
                { x => x.VersionId, value.VersionId }
            };
            await DialogService.ShowAsync<SecretValueDialog>("シークレット値", parameters);
        });

    private async Task RotateAsync(SecretInfo secret)
    {
        var dialogParams = new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "ローテーション確認" },
            { x => x.Message, $"シークレット「{secret.Name}」を即時ローテーションします。" }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("ローテーション確認", dialogParams);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await RunAsync("ローテーション中...", async (_, cancellationToken) =>
        {
            await Service.RotateSecretAsync(secret.Arn, cancellationToken);
            Snackbar.AddSuccess($"ローテーション開始: {secret.Name}");
        });
    }
}
