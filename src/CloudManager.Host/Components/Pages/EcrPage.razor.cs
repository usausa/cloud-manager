namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

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
            repositories = await Service.ListRepositoriesAsync();
        });

    private bool FilterFunc(EcrRepositoryInfo r) =>
        String.IsNullOrWhiteSpace(searchText) ||
        r.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        r.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
