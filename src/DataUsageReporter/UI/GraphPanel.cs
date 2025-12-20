using DataUsageReporter.Core;
using ScottPlot;
using ScottPlot.WinForms;

namespace DataUsageReporter.UI;

/// <summary>
/// Panel displaying network usage graphs with ScottPlot.
/// </summary>
public class GraphPanel : UserControl
{
    private readonly FormsPlot _plot;
    private readonly ComboBox _granularitySelector;
    private readonly IUsageAggregator _aggregator;
    private readonly ISpeedFormatter _formatter;
    private TimeGranularity _currentGranularity = TimeGranularity.Minute;

    public GraphPanel(IUsageAggregator aggregator, ISpeedFormatter formatter)
    {
        _aggregator = aggregator;
        _formatter = formatter;

        // Setup layout
        Dock = DockStyle.Fill;

        // Create granularity selector
        _granularitySelector = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Height = 25
        };
        _granularitySelector.Items.AddRange(new object[]
        {
            "Last 60 Minutes",
            "Last 24 Hours",
            "Last 30 Days",
            "Last 12 Months",
            "Last 5 Years"
        });
        _granularitySelector.SelectedIndex = 0; // Default to last 60 minutes
        _granularitySelector.SelectedIndexChanged += OnGranularityChanged;

        // Create plot
        _plot = new FormsPlot
        {
            Dock = DockStyle.Fill
        };

        // Add controls
        Controls.Add(_plot);
        Controls.Add(_granularitySelector);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _ = RefreshDataAsync();
    }

    private void OnGranularityChanged(object? sender, EventArgs e)
    {
        _currentGranularity = _granularitySelector.SelectedIndex switch
        {
            0 => TimeGranularity.Minute,
            1 => TimeGranularity.Hour,
            2 => TimeGranularity.Day,
            3 => TimeGranularity.Month,
            4 => TimeGranularity.Year,
            _ => TimeGranularity.Hour
        };

        _ = RefreshDataAsync();
    }

    public async Task RefreshDataAsync()
    {
        var (from, to) = GetDateRange(_currentGranularity);
        var dataPoints = await _aggregator.GetDataPointsAsync(from, to, _currentGranularity);

        UpdatePlot(dataPoints);
    }

    private static (DateTime from, DateTime to) GetDateRange(TimeGranularity granularity)
    {
        var now = DateTime.Now;
        return granularity switch
        {
            TimeGranularity.Minute => (now.AddMinutes(-60), now),
            TimeGranularity.Hour => (now.AddHours(-24), now),
            TimeGranularity.Day => (now.AddDays(-30), now),
            TimeGranularity.Month => (now.AddMonths(-12), now),
            TimeGranularity.Year => (now.AddYears(-5), now),
            _ => (now.AddHours(-24), now)
        };
    }

    private void UpdatePlot(IReadOnlyList<UsageDataPoint> dataPoints)
    {
        _plot.Plot.Clear();

        if (dataPoints.Count == 0)
        {
            _plot.Plot.Title("No data available");
            _plot.Refresh();
            return;
        }

        var timestamps = dataPoints.Select(d => d.Timestamp.ToOADate()).ToArray();
        // Convert bytes to Mbps (bytes * 8 / 1,000,000)
        var downloads = dataPoints.Select(d => (double)d.DownloadBytes * 8 / 1_000_000).ToArray();
        var uploads = dataPoints.Select(d => (double)d.UploadBytes * 8 / 1_000_000).ToArray();

        // Create scatter plots configured as line charts (no fill, no markers)
        var downloadScatter = _plot.Plot.Add.Scatter(timestamps, downloads);
        downloadScatter.LegendText = "Download";
        downloadScatter.LineStyle.Color = ScottPlot.Color.FromHex("#4CAF50"); // Green
        downloadScatter.LineStyle.Width = 2;
        downloadScatter.MarkerStyle.IsVisible = false;

        var uploadScatter = _plot.Plot.Add.Scatter(timestamps, uploads);
        uploadScatter.LegendText = "Upload";
        uploadScatter.LineStyle.Color = ScottPlot.Color.FromHex("#FF9800"); // Orange
        uploadScatter.LineStyle.Width = 2;
        uploadScatter.MarkerStyle.IsVisible = false;

        // Configure axes
        _plot.Plot.Axes.DateTimeTicksBottom();

        // Format Y axis label
        _plot.Plot.Axes.Left.Label.Text = "Mbps";
        _plot.Plot.Axes.Bottom.Label.Text = "Time";
        _plot.Plot.Title($"Network Usage - {GetGranularityLabel(_currentGranularity)}");
        _plot.Plot.ShowLegend(Alignment.UpperRight);

        _plot.Refresh();
    }

    private static string GetGranularityLabel(TimeGranularity granularity)
    {
        return granularity switch
        {
            TimeGranularity.Minute => "By Minute",
            TimeGranularity.Hour => "By Hour",
            TimeGranularity.Day => "By Day",
            TimeGranularity.Month => "By Month",
            TimeGranularity.Year => "By Year",
            _ => "Unknown"
        };
    }
}
