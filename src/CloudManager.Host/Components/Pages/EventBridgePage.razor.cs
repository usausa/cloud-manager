namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

public sealed partial class EventBridgePage
{
    [Inject]
    public required EventBridgeService Service { get; set; }

    private List<EventBridgeRuleInfo> rules = [];

    private string searchText = string.Empty;

    private IEnumerable<EventBridgeRuleInfo> FilteredRules =>
        String.IsNullOrWhiteSpace(searchText)
            ? rules
            : rules.Where(r => r.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            rules = await Service.ListRulesAsync(cancellationToken: CancellationToken);
        });

    private Task EnableAsync(EventBridgeRuleInfo rule) =>
        RunAsync("有効化中...", async (_, cancellationToken) =>
        {
            await Service.EnableRuleAsync(rule.Name, cancellationToken: cancellationToken);
            Snackbar.AddSuccess($"ルールを有効化しました: {rule.Name}");
            await LoadAsync();
        });

    private Task DisableAsync(EventBridgeRuleInfo rule) =>
        RunAsync("無効化中...", async (_, cancellationToken) =>
        {
            await Service.DisableRuleAsync(rule.Name, cancellationToken: cancellationToken);
            Snackbar.AddSuccess($"ルールを無効化しました: {rule.Name}");
            await LoadAsync();
        });
}
