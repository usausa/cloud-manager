namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class RdsParamGroupDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string GroupName { get; set; } = string.Empty;

    [Inject]
    public required RdsParamGroupService Service { get; set; }

    private List<RdsParameterInfo> parameters = [];

    private string search = string.Empty;

    private IEnumerable<RdsParameterInfo> Filtered =>
        String.IsNullOrWhiteSpace(search) ? parameters
            : parameters.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            parameters = await Service.ListParametersAsync(GroupName, CancellationToken);
        });
}
