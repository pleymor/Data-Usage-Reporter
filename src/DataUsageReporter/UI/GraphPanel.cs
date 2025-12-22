using DataUsageReporter.Core;
using DataUsageReporter.Core.Localization;
using ScottPlot;
using ScottPlot.Plottables;
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
    private double[] _timestamps = Array.Empty<double>();
    private double[] _downloads = Array.Empty<double>();
    private double[] _uploads = Array.Empty<double>();
    private bool _isAutoScaling = false;
    private readonly List<VerticalSpan> _timeSpans = new();
    private readonly List<VerticalLine> _timeLines = new();
    private DisplayGranularity _currentGranularity = DisplayGranularity.Hour;

    private enum DisplayGranularity { Day, Hour, Minute, Second }

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

        // Subscribe to axes changed to auto-fit Y based on visible X range
        _plot.Plot.RenderManager.AxisLimitsChanged += OnAxisLimitsChanged;

        // Add controls
        Controls.Add(_plot);
    }

    private void OnAxisLimitsChanged(object? sender, RenderDetails e)
    {
        // Prevent recursive calls
        if (_isAutoScaling || _timestamps.Length == 0)
            return;

        // Get current X-axis limits
        var xMin = _plot.Plot.Axes.Bottom.Range.Min;
        var xMax = _plot.Plot.Axes.Bottom.Range.Max;

        // Find data points within visible X range
        double yMin = double.MaxValue;
        double yMax = double.MinValue;

        for (int i = 0; i < _timestamps.Length; i++)
        {
            if (_timestamps[i] >= xMin && _timestamps[i] <= xMax)
            {
                var download = _downloads[i];
                var upload = _uploads[i];

                if (download < yMin) yMin = download;
                if (download > yMax) yMax = download;
                if (upload < yMin) yMin = upload;
                if (upload > yMax) yMax = upload;
            }
        }

        if (yMin != double.MaxValue && yMax != double.MinValue)
        {
            // Add 10% padding
            var padding = (yMax - yMin) * 0.1;
            if (padding == 0) padding = 1;

            _isAutoScaling = true;
            _plot.Plot.Axes.Left.Range.Set(Math.Max(0, yMin - padding), yMax + padding);
            _isAutoScaling = false;
        }

        // Update time separators based on zoom level
        UpdateTimeSeparators(xMin, xMax);

        // Update X-axis tick format based on zoom level
        UpdateAxisTickFormat(xMin, xMax);
    }

    private DisplayGranularity DetermineGranularity(double xMin, double xMax)
    {
        try
        {
            var minDate = DateTime.FromOADate(xMin);
            var maxDate = DateTime.FromOADate(xMax);
            var range = maxDate - minDate;

            if (range.TotalDays > 7)
                return DisplayGranularity.Day;
            if (range.TotalHours > 6)
                return DisplayGranularity.Hour;
            if (range.TotalMinutes > 10)
                return DisplayGranularity.Minute;
            return DisplayGranularity.Second;
        }
        catch
        {
            return DisplayGranularity.Hour; // Safe default
        }
    }

    private void UpdateTimeSeparators(double xMin, double xMax)
    {
        try
        {
            // Validate OADate range (valid range is roughly 1900-9999)
            if (xMin < 1 || xMax < 1 || xMin > 2958465 || xMax > 2958465 || xMin >= xMax)
                return;

            var newGranularity = DetermineGranularity(xMin, xMax);

            // Only rebuild if granularity changed
            if (newGranularity == _currentGranularity && _timeSpans.Count > 0)
                return;

            _currentGranularity = newGranularity;

            // Remove existing spans and lines
            foreach (var span in _timeSpans)
            {
                _plot.Plot.Remove(span);
            }
            _timeSpans.Clear();

            foreach (var line in _timeLines)
            {
                _plot.Plot.Remove(line);
            }
            _timeLines.Clear();

            var minDate = DateTime.FromOADate(xMin);
            var maxDate = DateTime.FromOADate(xMax);

            // Add alternating bands for finest granularity
            var bandColor = ScottPlot.Color.FromHex("#F5F5F5");
            AddBandLayer(minDate, maxDate, newGranularity, bandColor);

            // Add vertical lines for coarser granularities with increasing thickness
            switch (newGranularity)
            {
                case DisplayGranularity.Hour:
                    AddLineLayer(minDate, maxDate, DisplayGranularity.Day, ScottPlot.Color.FromHex("#555555"), 4f);
                    break;

                case DisplayGranularity.Minute:
                    AddLineLayer(minDate, maxDate, DisplayGranularity.Hour, ScottPlot.Color.FromHex("#BBBBBB"), 1f);
                    AddLineLayer(minDate, maxDate, DisplayGranularity.Day, ScottPlot.Color.FromHex("#333333"), 5f);
                    break;

                case DisplayGranularity.Second:
                    AddLineLayer(minDate, maxDate, DisplayGranularity.Minute, ScottPlot.Color.FromHex("#CCCCCC"), 1f);
                    AddLineLayer(minDate, maxDate, DisplayGranularity.Hour, ScottPlot.Color.FromHex("#777777"), 3f);
                    AddLineLayer(minDate, maxDate, DisplayGranularity.Day, ScottPlot.Color.FromHex("#222222"), 6f);
                    break;
            }
        }
        catch
        {
            // Silently ignore separator errors - graph still works without them
        }
    }

    private void AddBandLayer(DateTime minDate, DateTime maxDate, DisplayGranularity granularity, ScottPlot.Color bandColor)
    {
        var boundaries = GetTimeBoundaries(minDate, maxDate, granularity);

        // Limit number of spans to prevent performance issues
        if (boundaries.Count > 500)
            return;

        bool isShaded = false;
        for (int i = 0; i < boundaries.Count - 1; i++)
        {
            if (isShaded)
            {
                var span = _plot.Plot.Add.VerticalSpan(
                    boundaries[i].ToOADate(),
                    boundaries[i + 1].ToOADate());
                span.FillStyle.Color = bandColor;
                span.LineStyle.IsVisible = false;
                _timeSpans.Add(span);

                // Move span to back so data lines are visible
                _plot.Plot.MoveToBack(span);
            }
            isShaded = !isShaded;
        }
    }

    private void AddLineLayer(DateTime minDate, DateTime maxDate, DisplayGranularity granularity, ScottPlot.Color lineColor, float lineWidth)
    {
        var boundaries = GetTimeBoundaries(minDate, maxDate, granularity);

        // Limit number of lines to prevent performance issues
        if (boundaries.Count > 200)
            return;

        foreach (var boundary in boundaries)
        {
            var line = _plot.Plot.Add.VerticalLine(boundary.ToOADate());
            line.LineStyle.Color = lineColor;
            line.LineStyle.Width = lineWidth;
            _timeLines.Add(line);

            // Move line to back so data lines are visible on top
            _plot.Plot.MoveToBack(line);
        }
    }

    private void UpdateAxisTickFormat(double xMin, double xMax)
    {
        try
        {
            var granularity = DetermineGranularity(xMin, xMax);
            var minDate = DateTime.FromOADate(xMin);
            var maxDate = DateTime.FromOADate(xMax);

            // Update X-axis label with date range
            string dateLabel;
            if (minDate.Date == maxDate.Date)
            {
                dateLabel = minDate.ToString("dddd dd MMM yyyy");
            }
            else
            {
                dateLabel = $"{minDate:dd MMM} - {maxDate:dd MMM yyyy}";
            }
            _plot.Plot.Axes.Bottom.Label.Text = dateLabel;

            // Simple time format for ticks
            string format = granularity switch
            {
                DisplayGranularity.Day => "dd MMM",
                DisplayGranularity.Hour => "HH:mm",
                DisplayGranularity.Minute => "HH:mm",
                DisplayGranularity.Second => "HH:mm:ss",
                _ => "HH:mm"
            };

            _plot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic
            {
                LabelFormatter = d => DateTime.FromOADate(d).ToString(format)
            };
        }
        catch
        {
            // Ignore formatting errors
        }
    }

    private List<DateTime> GetTimeBoundaries(DateTime minDate, DateTime maxDate, DisplayGranularity granularity)
    {
        var boundaries = new List<DateTime>();
        const int maxBoundaries = 1000;

        // Start from the boundary before minDate
        DateTime current = granularity switch
        {
            DisplayGranularity.Day => new DateTime(minDate.Year, minDate.Month, minDate.Day),
            DisplayGranularity.Hour => new DateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, 0, 0),
            DisplayGranularity.Minute => new DateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, 0),
            DisplayGranularity.Second => new DateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second),
            _ => minDate
        };

        // Add boundaries until we pass maxDate (with safety limit)
        while (current <= maxDate.AddSeconds(1) && boundaries.Count < maxBoundaries)
        {
            boundaries.Add(current);
            current = granularity switch
            {
                DisplayGranularity.Day => current.AddDays(1),
                DisplayGranularity.Hour => current.AddHours(1),
                DisplayGranularity.Minute => current.AddMinutes(1),
                DisplayGranularity.Second => current.AddSeconds(1),
                _ => current.AddHours(1)
            };
        }

        return boundaries;
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
        // Get all raw data points for full zoom capability
        var from = DateTime.Now.AddYears(-5);
        var to = DateTime.Now;

        var dataPoints = await _aggregator.GetDataPointsAsync(from, to, TimeGranularity.Minute);

        UpdatePlot(dataPoints);
    }

    private void UpdatePlot(IReadOnlyList<UsageDataPoint> dataPoints)
    {
        _plot.Plot.Clear();
        _timeSpans.Clear();

        if (dataPoints.Count == 0)
        {
            _timestamps = Array.Empty<double>();
            _downloads = Array.Empty<double>();
            _uploads = Array.Empty<double>();
            _plot.Plot.Title(_localization.GetString("Graph_NoData"));
            _plot.Refresh();
            return;
        }

        // Store data for Y-axis auto-scaling
        _timestamps = dataPoints.Select(d => d.Timestamp.ToOADate()).ToArray();
        // Convert bytes to Mbps (bytes * 8 / 1,000,000)
        _downloads = dataPoints.Select(d => (double)d.DownloadBytes * 8 / 1_000_000).ToArray();
        _uploads = dataPoints.Select(d => (double)d.UploadBytes * 8 / 1_000_000).ToArray();

        // Create scatter plots configured as line charts (no fill, no markers)
        var downloadScatter = _plot.Plot.Add.Scatter(_timestamps, _downloads);
        downloadScatter.LegendText = _localization.GetString("Label_Download");
        downloadScatter.LineStyle.Color = ScottPlot.Color.FromHex("#4CAF50"); // Green
        downloadScatter.LineStyle.Width = 2;
        downloadScatter.MarkerStyle.IsVisible = false;

        var uploadScatter = _plot.Plot.Add.Scatter(_timestamps, _uploads);
        uploadScatter.LegendText = _localization.GetString("Label_Upload");
        uploadScatter.LineStyle.Color = ScottPlot.Color.FromHex("#FF9800"); // Orange
        uploadScatter.LineStyle.Width = 2;
        uploadScatter.MarkerStyle.IsVisible = false;

        // Configure axes (custom tick format set in UpdateAxisTickFormat)

        // Format Y axis label (X-axis label set dynamically in UpdateAxisTickFormat)
        _plot.Plot.Axes.Left.Label.Text = _localization.GetString("Graph_Mbps");
        _plot.Plot.Title(_localization.GetString("Graph_Title"));
        _plot.Plot.ShowLegend(Alignment.UpperRight);

        // Set initial view to current day (00:00 to 00:00)
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        _plot.Plot.Axes.SetLimitsX(today.ToOADate(), tomorrow.ToOADate());
        _plot.Plot.Axes.AutoScaleY();

        // Draw initial time separators and set tick format
        _currentGranularity = (DisplayGranularity)(-1); // Force redraw
        UpdateTimeSeparators(today.ToOADate(), tomorrow.ToOADate());
        UpdateAxisTickFormat(today.ToOADate(), tomorrow.ToOADate());

        _plot.Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from events to prevent memory leaks
            if (_plot?.Plot?.RenderManager != null)
            {
                _plot.Plot.RenderManager.AxisLimitsChanged -= OnAxisLimitsChanged;
            }
        }
        base.Dispose(disposing);
    }
}
