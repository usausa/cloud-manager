namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class AcmPage
{
    [Inject]
    public required AcmService Service { get; set; }

    private List<AcmCertificateInfo> certificates = [];

    private string searchText = string.Empty;

    private IEnumerable<AcmCertificateInfo> FilteredCertificates =>
        String.IsNullOrWhiteSpace(searchText)
            ? certificates
            : certificates.Where(c =>
                c.DomainName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                c.Status.Contains(searchText, StringComparison.OrdinalIgnoreCase));

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            certificates = await Service.ListCertificatesAsync(CancellationToken);
        });

    private async Task ShowDetailAsync(AcmCertificateInfo cert)
    {
        AcmCertificateDetail detail;
        try
        {
            detail = await Service.GetCertificateAsync(cert.Arn, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
            return;
        }

        var dialogParams = new DialogParameters<AcmCertificateDetailDialog>
        {
            { x => x.Detail, detail }
        };
        await DialogService.ShowAsync<AcmCertificateDetailDialog>(cert.DomainName, dialogParams);
    }
}
