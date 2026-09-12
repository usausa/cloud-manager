namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Application;
using CloudManager.Host.Infrastructure.Components;
using CloudManager.Host.Infrastructure.Notifications;

using Microsoft.AspNetCore.Components;
using Microsoft.FeatureManagement;

using MudBlazor;

public sealed partial class Home
{
    private string? lastNotification;

    private bool featureEnabled;

    [Inject]
    public required NotificationBus NotificationBus { get; set; }

    [Inject]
    public required IFeatureManager FeatureManager { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Subscribe server notification (unsubscribed on dispose)
        NotificationBus.Received += OnNotificationReceived;

        // Feature flag example
        featureEnabled = await FeatureManager.IsEnabledAsync(FeatureFlags.CustomOption);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NotificationBus.Received -= OnNotificationReceived;
        }

        base.Dispose(disposing);
    }

    private void OnNotificationReceived(object? sender, NotificationEventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            lastNotification = e.Message;
            Snackbar.AddInfo(e.Message);
            StateHasChanged();
        });
    }
}
