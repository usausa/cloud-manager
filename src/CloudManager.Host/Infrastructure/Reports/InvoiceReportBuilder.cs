namespace CloudManager.Host.Infrastructure.Reports;

using OysterReport;

public sealed class InvoiceReportBuilder
{
    private const string TemplatePath = "Assets/Reports/Invoice.xlsx";
    private const string FontPath = "Assets/Fonts/ipaexg.ttf";

    private readonly EmbeddedFontResolver fontResolver = new(FontPath);

    private readonly TimeProvider timeProvider;

    public InvoiceReportBuilder(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public byte[] Build(IReadOnlyList<DataEntity> entities)
    {
        var engine = new OysterReportEngine
        {
            FontResolver = fontResolver
        };

        using var workbook = new TemplateWorkbook(TemplatePath);
        EditSheet(workbook.Sheets[0], entities);

        using var output = new MemoryStream();
        engine.GeneratePdf(workbook, output);
        return output.ToArray();
    }

    private void EditSheet(TemplateSheet sheet, IReadOnlyList<DataEntity> entities)
    {
        var today = timeProvider.GetLocalNow();

        sheet.ReplacePlaceholders(new Dictionary<string, string?>
        {
            ["Subject"] = "御請求書",
            ["BillingTo"] = "サンプル株式会社",
            ["InvoiceDate"] = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["InvoiceNo"] = $"INV-{today:yyyyMMdd}-001",
            ["DeliveryDate"] = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        });

        sheet.ReplacePlaceholders(entities.Select(static (e, i) => new Dictionary<string, string?>
        {
            ["No"] = (i + 1).ToString(CultureInfo.InvariantCulture),
            ["Item"] = e.Name,
            ["Qty"] = "1",
            ["Price"] = e.Value.ToString("N0", CultureInfo.InvariantCulture),
            ["Amount"] = e.Value.ToString("N0", CultureInfo.InvariantCulture)
        }));

        var subTotal = entities.Sum(static e => (long)e.Value);
        var tax = (long)(subTotal * 0.1);
        sheet.ReplacePlaceholders(new Dictionary<string, string?>
        {
            ["SubTotal"] = subTotal.ToString("N0", CultureInfo.InvariantCulture),
            ["Tax"] = tax.ToString("N0", CultureInfo.InvariantCulture),
            ["TotalAmount"] = (subTotal + tax).ToString("N0", CultureInfo.InvariantCulture)
        });
    }
}
