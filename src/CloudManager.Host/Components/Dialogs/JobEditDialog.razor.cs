namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Models.Forms;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class JobEditDialog
{
    private static readonly JobFormValidator Validator = new();

    private MudForm form = default!;

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required JobForm Form { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    // Reset the operation to the first one of the selected service
    private void OnServiceTypeChanged(JobServiceType serviceType)
    {
        Form.ServiceType = serviceType;
        Form.Operation = JobOperationCatalog.ForService(serviceType)[0];
    }

    private async Task OnOkClick()
    {
        await form.ValidateAsync();
        if (form.IsValid)
        {
            MudDialog.Close(DialogResult.Ok(Form));
        }
    }

    private void OnCancelClick() => MudDialog.Cancel();
}
