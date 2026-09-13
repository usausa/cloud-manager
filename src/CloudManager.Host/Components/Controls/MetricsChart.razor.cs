namespace CloudManager.Host.Components.Controls;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// CloudWatch メトリクスを折れ線で表示する
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

        // 全シリーズ共通の時刻をX軸にし、欠けている点は0で埋める
        var timestamps = Series
            .SelectMany(static x => x.Points.Select(static p => p.Timestamp))
            .Distinct()
            .Order()
            .ToList();

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
    }
}
