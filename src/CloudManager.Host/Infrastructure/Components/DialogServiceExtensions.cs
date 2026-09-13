namespace CloudManager.Host.Infrastructure.Components;

using CloudManager.Host.Components.Dialogs;

using MudBlazor;

public static class DialogServiceExtensions
{
    public static async ValueTask ShowInformation(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<AppMessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(AppMessageBox.Type), MessageBoxType.Information },
                { nameof(AppMessageBox.Title), title },
                { nameof(AppMessageBox.Message), message }
            },
            null);
        await reference.Result;
    }

    public static async ValueTask<bool> ShowConfirm(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<AppMessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(AppMessageBox.Type), MessageBoxType.Confirm },
                { nameof(AppMessageBox.Title), title },
                { nameof(AppMessageBox.Message), message }
            },
            null);
        var result = await reference.Result;
        return (bool?)result!.Data == true;
    }

    // Confirms a dangerous operation, returning null when cancelled
    public static async ValueTask<ConfirmResult?> ShowOperationConfirm(
        this IDialogService dialog,
        string title,
        string message,
        bool showForce = false,
        string? requireConfirmText = null)
    {
        var reference = await dialog.ShowAsync<ConfirmDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(ConfirmDialog.Title), title },
                { nameof(ConfirmDialog.Message), message },
                { nameof(ConfirmDialog.ShowForce), showForce },
                { nameof(ConfirmDialog.RequireConfirmText), requireConfirmText }
            });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (ConfirmResult)result.Data! : null;
    }
}
