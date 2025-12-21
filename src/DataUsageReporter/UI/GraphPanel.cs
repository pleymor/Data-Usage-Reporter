using DataUsageReporter.Core;
using DataUsageReporter.Core.Localization;
using ScottPlot;
using ScottPlot.WinForms;

namespace DataUsageReporter.UI;

/// <summary>
/// Panel displaying network usage as a unified graph with ScottPlot.
/// </summary>
/// <remarks>
/// Design Decision: This component uses a SINGLE unified graph to display both
/// download and upload data together, showing ALL historical data without
/// time range selection. This approach was chosen to:
/// - Simplify the user experience by removing dropdown complexity
/// - Show complete usage history in one view
/// - Enable easy comparison between download and upload at any point in time
/// - Use distinct colors (green for download, orange for upload) for clarity
///
/// See specs/005-unified-graph/spec.md for the full specification.
/// </remarks>
public class GraphPanel : UserControl
{
    private readonly FormsPlot _plot;
    private readonly IUsageAggregator _aggregator;
    private readonly ISpeedFormatter _formatter;
    private readonly ILocalizationService _localization;

    public GraphPanel(IUsageAggregator aggregator, ISpeedFormatter formatter, ILocalizationService localization)
    {
        _aggregator = aggregator;
        _formatter = formatter;
        _localization = localization;

        // Setup layout
        Dock = DockStyle.Fill;

        // Create plot (no dropdown - shows all data)
        _plot = new FormsPlot
        {
            Dock = DockStyle.Fill
        };

        // Add controls
        Controls.Add(_plot);
    }

    public void RefreshStrings()
    {
        // Re-render the plot with updated localized strings
        _ = RefreshDataAsync();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _ = RefreshDataAsync();
    }

    public async Task RefreshDataAsync()
    {
        // Fetch all data (up to 5 years back covers all retained data)
        var from = DateTime.Now.AddYears(-5);
        var to = DateTime.Now;

        // Use Minute granularity for finest detail
        var dataPoints = await _aggregator.GetDataPointsAsync(from, to, TimeGranularity.Minute);

        UpdatePlot(dataPoints);
    }

    private void UpdatePlot(IReadOnlyList<UsageDataPoint> dataPoints)
    {
        _plot.Plot.Clear();

        if (dataPoints.Count == 0)
        {
            _plot.Plot.Title(_localization.GetString("Graph_NoData"));
            _plot.Refresh();
            return;
        }

        var timestamps = dataPoints.Select(d => d.Timestamp.ToOADate()).ToArray();
        // Convert bytes to Mbps (bytes * 8 / 1,000,000)
        var downloads = dataPoints.Select(d => (double)d.DownloadBytes * 8 / 1_000_000).ToArray();
        var uploads = dataPoints.Select(d => (double)d.UploadBytes * 8 / 1_000_000).ToArray();

        // Create scatter plots configured as line charts (no fill, no markers)
        var downloadScatter = _plot.Plot.Add.Scatter(timestamps, downloads);
        downloadScatter.LegendText = _localization.GetString("Label_Download");
        downloadScatter.LineStyle.Color = ScottPlot.Color.FromHex("#4CAF50"); // Green
        downloadScatter.LineStyle.Width = 2;
        downloadScatter.MarkerStyle.IsVisible = false;

        var uploadScatter = _plot.Plot.Add.Scatter(timestamps, uploads);
        uploadScatter.LegendText = _localization.GetString("Label_Upload");
        uploadScatter.LineStyle.Color = ScottPlot.Color.FromHex("#FF9800"); // Orange
        uploadScatter.LineStyle.Width = 2;
        uploadScatter.MarkerStyle.IsVisible = false;

        // Configure axes
        _plot.Plot.Axes.DateTimeTicksBottom();

        // Format Y axis label
        _plot.Plot.Axes.Left.Label.Text = _localization.GetString("Graph_Mbps");
        _plot.Plot.Axes.Bottom.Label.Text = _localization.GetString("Graph_Time");
        _plot.Plot.Title(_localization.GetString("Graph_Title"));
        _plot.Plot.ShowLegend(Alignment.UpperRight);

        _plot.Refresh();
    }
}
