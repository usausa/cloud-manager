namespace CloudManager.Host.Components.Controls;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// Shows CloudWatch metrics as a line chart
public sealed partial class MetricsChart
{
    private readonly LineChartOptions chartOptions = new() { YAxisTicks = 5 };

    private List<ChartSeries<double>> chartSeries = [];

    private string[] labels = [];

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Unit { get; set; }

    [Parameter]
    public IReadOnlyList<MetricsChartSeries>? Series { get; set; }

    protected override void OnParametersSet()
    {
        if ((Series is null) || (Series.Count == 0))
        {
            chartSeries = [];
            labels = [];
            return;
        }

        // Use the union of timestamps as the X axis and fill missing points with 0
        var timestamps = Series
            .SelectMany(static x => x.Points.Select(static p => p.Timestamp))
            .Distinct()
            .Order()
            .ToList();

#pragma warning disable IDE0028
        labels = timestamps
            .Select(static x => x.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture))
            .ToArray();

        chartSeries = Series
            .Select(x =>
            {
                var lookup = x.Points.ToDictionary(static p => p.Timestamp, static p => p.Value);
                return new ChartSeries<double>
                {
                    Name = x.Label,
                    Data = timestamps.Select(t => lookup.GetValueOrDefault(t)).ToArray()
                };
            })
            .ToList();
#pragma warning restore IDE0028
    }
}
