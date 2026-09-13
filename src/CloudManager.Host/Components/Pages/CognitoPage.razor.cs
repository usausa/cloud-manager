namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

public sealed partial class CognitoPage
{
    [Inject]
    public required CognitoService Service { get; set; }

    private List<UserPoolInfo> pools = [];

    private List<CognitoUserInfo> users = [];

    private UserPoolInfo? selectedPool;

    private bool isUsersLoading;

    private string userSearchText = string.Empty;

    private IEnumerable<CognitoUserInfo> FilteredUsers =>
        String.IsNullOrWhiteSpace(userSearchText)
            ? users
            : users.Where(u =>
                u.Username.Contains(userSearchText, StringComparison.OrdinalIgnoreCase) ||
                (u.Email ?? string.Empty).Contains(userSearchText, StringComparison.OrdinalIgnoreCase));

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedPool = null;
        users = [];
        return LoadAsync(async () =>
        {
            pools = await Service.ListUserPoolsAsync(CancellationToken);
        });
    }

    private Task OnPoolSelectedAsync(UserPoolInfo? pool)
    {
        selectedPool = pool;
        users = [];
        if (pool is not null)
        {
            return LoadUsersAsync();
        }
        return Task.CompletedTask;
    }

    private Task LoadUsersAsync()
    {
        if (selectedPool is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            users = await Service.ListUsersAsync(selectedPool.Id, CancellationToken);
        }, x => isUsersLoading = x);
    }

    private async Task ResetPasswordAsync(CognitoUserInfo user)
    {
        if (selectedPool is null)
        {
            return;
        }
        if (await DialogService.ShowOperationConfirm("パスワードリセット", $"ユーザー「{user.Username}」のパスワードをリセットしますか？", requireConfirmText: user.Username) is null)
        {
            return;
        }

        await RunAsync("パスワードリセット中...", async (_, cancellationToken) =>
        {
            await Service.AdminResetPasswordAsync(selectedPool.Id, user.Username, cancellationToken);
            Snackbar.AddSuccess($"{user.Username} のパスワードをリセットしました。");
        });
    }
}
