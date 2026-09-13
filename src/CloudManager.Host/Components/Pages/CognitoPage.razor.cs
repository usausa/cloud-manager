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

    private async Task LoadUsersAsync()
    {
        if (selectedPool is null)
        {
            return;
        }
        isUsersLoading = true;
        try
        {
            users = await Service.ListUsersAsync(selectedPool.Id, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isUsersLoading = false;
        }
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

        await RunAsync("実行中...", async (_, cancellationToken) =>
        {
            await Service.AdminResetPasswordAsync(selectedPool.Id, user.Username, cancellationToken);
            Snackbar.AddSuccess($"パスワードをリセットしました: {user.Username}");
        });
    }
}
