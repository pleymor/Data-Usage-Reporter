using DataUsageReporter.Core;
using DataUsageReporter.Core.Localization;
using ScottPlot;

namespace DataUsageReporter.Email;

/// <summary>
/// Renders usage graphs as PNG images for email reports using ScottPlot.
/// </summary>
public class EmailReportGraphRenderer : IEmailReportGraphRenderer
{
    private readonly ILocalizationService _localization;
    private const int GraphWidth = 600;
    private const int GraphHeight = 300;
    private const string DownloadColor = "#4285f4";
    private const string UploadColor = "#ea4335";

    public EmailReportGraphRenderer(ILocalizationService localization)
    {
        _localization = localization;
    }

    public byte[] RenderHourlyGraph(IReadOnlyList<UsageDataPoint> dataPoints, DateTime date)
    {
        var plot = new Plot();

        // Prepare data - ensure 24 hours
        var positions = Enumerable.Range(0, 24).Select(h => (double)h).ToArray();
        var downloads = new double[24];
        var uploads = new double[24];

        foreach (var dp in dataPoints)
        {
            var hour = dp.Timestamp.Hour;
            if (hour >= 0 && hour < 24)
            {
                downloads[hour] += dp.DownloadBytes / 1_048_576.0; // Convert to MB
                uploads[hour] += dp.UploadBytes / 1_048_576.0;
            }
        }

        // Create stacked bar chart using two separate bar series
        // Download bars (bottom)
        var downloadBars = plot.Add.Bars(positions, downloads);
        downloadBars.Color = ScottPlot.Color.FromHex(DownloadColor);
        downloadBars.LegendText = _localization.GetString("Label_Download");

        // Upload bars (stacked on top) - set each bar's base to the download value
        var uploadBarsList = new List<Bar>();
        for (int i = 0; i < 24; i++)
        {
            uploadBarsList.Add(new Bar
            {
                Position = i,
                ValueBase = downloads[i],
                Value = downloads[i] + uploads[i],
                FillColor = ScottPlot.Color.FromHex(UploadColor)
            });
        }
        var uploadBars = plot.Add.Bars(uploadBarsList.ToArray());
        uploadBars.LegendText = _localization.GetString("Label_Upload");

        // Configure axes
        plot.Title(_localization.GetString("Email_GraphHourly"));
        plot.YLabel("MB");
        plot.XLabel(_localization.GetString("Graph_Time"));

        // Set X axis ticks for hours
        var ticks = Enumerable.Range(0, 24).Where(h => h % 3 == 0)
            .Select(h => new Tick(h, $"{h:00}:00")).ToArray();
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

        plot.ShowLegend(Alignment.UpperRight);

        return plot.GetImageBytes(GraphWidth, GraphHeight, ImageFormat.Png);
    }

    public byte[] RenderDailyGraph(IReadOnlyList<UsageDataPoint> dataPoints, DateTime periodEnd)
    {
        var plot = new Plot();

        // Prepare data for last 7 days
        var dates = Enumerable.Range(0, 7).Select(i => periodEnd.Date.AddDays(-6 + i)).ToList();
        var positions = Enumerable.Range(0, 7).Select(i => (double)i).ToArray();
        var downloads = new double[7];
        var uploads = new double[7];

        foreach (var dp in dataPoints)
        {
            var index = dates.FindIndex(d => d.Date == dp.Timestamp.Date);
            if (index >= 0 && index < 7)
            {
                downloads[index] += dp.DownloadBytes / 1_048_576.0; // Convert to MB
                uploads[index] += dp.UploadBytes / 1_048_576.0;
            }
        }

        // Download bars (bottom)
        var downloadBars = plot.Add.Bars(positions, downloads);
        downloadBars.Color = ScottPlot.Color.FromHex(DownloadColor);
        downloadBars.LegendText = _localization.GetString("Label_Download");

        // Upload bars (stacked on top)
        var uploadBarsList = new List<Bar>();
        for (int i = 0; i < 7; i++)
        {
            uploadBarsList.Add(new Bar
            {
                Position = i,
                ValueBase = downloads[i],
                Value = downloads[i] + uploads[i],
                FillColor = ScottPlot.Color.FromHex(UploadColor)
            });
        }
        var uploadBars = plot.Add.Bars(uploadBarsList.ToArray());
        uploadBars.LegendText = _localization.GetString("Label_Upload");

        // Configure axes
        plot.Title(_localization.GetString("Email_GraphDaily"));
        plot.YLabel("MB");

        // Set X axis ticks for days
        var ticks = dates.Select((d, i) => new Tick(i, d.ToString("MM/dd"))).ToArray();
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

        plot.ShowLegend(Alignment.UpperRight);

        return plot.GetImageBytes(GraphWidth, GraphHeight, ImageFormat.Png);
    }

    public byte[] RenderWeeklyGraph(IReadOnlyList<UsageDataPoint> dataPoints, DateTime periodEnd)
    {
        var plot = new Plot();

        // Prepare data for last 5 weeks
        var positions = Enumerable.Range(0, 5).Select(i => (double)i).ToArray();
        var downloads = new double[5];
        var uploads = new double[5];
        var weekLabels = new string[5];

        for (int i = 0; i < 5; i++)
        {
            var weekEnd = periodEnd.Date.AddDays(-7 * (4 - i));
            var weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(weekEnd);
            weekLabels[i] = _localization.GetString("Email_Week", weekNumber);
        }

        foreach (var dp in dataPoints)
        {
            // Find which week this data point belongs to
            for (int i = 0; i < 5; i++)
            {
                var weekEnd = periodEnd.Date.AddDays(-7 * (4 - i));
                var weekStart = weekEnd.AddDays(-6);
                if (dp.Timestamp.Date >= weekStart && dp.Timestamp.Date <= weekEnd)
                {
                    downloads[i] += dp.DownloadBytes / 1_048_576.0; // Convert to MB
                    uploads[i] += dp.UploadBytes / 1_048_576.0;
                    break;
                }
            }
        }

        // Download bars (bottom)
        var downloadBars = plot.Add.Bars(positions, downloads);
        downloadBars.Color = ScottPlot.Color.FromHex(DownloadColor);
        downloadBars.LegendText = _localization.GetString("Label_Download");

        // Upload bars (stacked on top)
        var uploadBarsList = new List<Bar>();
        for (int i = 0; i < 5; i++)
        {
            uploadBarsList.Add(new Bar
            {
                Position = i,
                ValueBase = downloads[i],
                Value = downloads[i] + uploads[i],
                FillColor = ScottPlot.Color.FromHex(UploadColor)
            });
        }
        var uploadBars = plot.Add.Bars(uploadBarsList.ToArray());
        uploadBars.LegendText = _localization.GetString("Label_Upload");

        // Configure axes
        plot.Title(_localization.GetString("Email_GraphWeekly"));
        plot.YLabel("MB");

        // Set X axis ticks for weeks
        var ticks = weekLabels.Select((label, i) => new Tick(i, label)).ToArray();
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

        plot.ShowLegend(Alignment.UpperRight);

        return plot.GetImageBytes(GraphWidth, GraphHeight, ImageFormat.Png);
    }
}
