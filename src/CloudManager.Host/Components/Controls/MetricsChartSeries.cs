namespace CloudManager.Host.Components.Controls;

public sealed record MetricsChartSeries(string Label, IReadOnlyList<CloudWatchDataPoint> Points);
