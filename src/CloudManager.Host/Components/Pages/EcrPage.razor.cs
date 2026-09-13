namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

public sealed partial class EcrPage
{
    [Inject]
    public required EcrService Service { get; set; }

    private List<EcrRepositoryInfo> repositories = [];

    private string searchText = string.Empty;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            repositories = await Service.ListRepositoriesAsync(CancellationToken);
        });

    private bool FilterFunc(EcrRepositoryInfo r) =>
        String.IsNullOrWhiteSpace(searchText) ||
        r.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        r.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
