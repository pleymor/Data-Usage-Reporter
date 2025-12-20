using DataUsageReporter.Data;

namespace DataUsageReporter.Email;

/// <summary>
/// Timer-based report scheduler that sends reports at configured times.
/// </summary>
public class Scheduler : IReportScheduler
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IReportGenerator _reportGenerator;
    private readonly IEmailSender _emailSender;
    private System.Threading.Timer? _timer;
    private ReportSchedule? _schedule;

    public event EventHandler<ReportEventArgs>? ReportCompleted;

    public Scheduler(
        ISettingsRepository settingsRepository,
        IReportGenerator reportGenerator,
        IEmailSender emailSender)
    {
        _settingsRepository = settingsRepository;
        _reportGenerator = reportGenerator;
        _emailSender = emailSender;
    }

    public void Start()
    {
        _schedule = _settingsRepository.LoadSchedule();

        if (_schedule == null || !_schedule.IsEnabled)
        {
            return;
        }

        ScheduleNextRun();
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _timer = null;
    }

    public DateTime? GetNextRunTime()
    {
        if (_schedule == null || !_schedule.IsEnabled)
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(_schedule.NextRunTime).LocalDateTime;
    }

    public void RecalculateNextRun()
    {
        _schedule = _settingsRepository.LoadSchedule();
        if (_schedule == null || !_schedule.IsEnabled)
        {
            Stop();
            return;
        }

        CalculateAndSaveNextRunTime();
        ScheduleNextRun();
    }

    private void ScheduleNextRun()
    {
        if (_schedule == null || !_schedule.IsEnabled)
            return;

        var nextRun = DateTimeOffset.FromUnixTimeSeconds(_schedule.NextRunTime).LocalDateTime;
        var delay = nextRun - DateTime.Now;

        if (delay < TimeSpan.Zero)
        {
            // Already past, recalculate
            CalculateAndSaveNextRunTime();
            nextRun = DateTimeOffset.FromUnixTimeSeconds(_schedule.NextRunTime).LocalDateTime;
            delay = nextRun - DateTime.Now;
        }

        _timer?.Dispose();
        _timer = new System.Threading.Timer(
            OnTimerElapsed,
            null,
            (long)delay.TotalMilliseconds,
            Timeout.Infinite);
    }

    private async void OnTimerElapsed(object? state)
    {
        if (_schedule == null)
            return;

        var (periodStart, periodEnd) = GetReportPeriod(_schedule.Frequency);

        try
        {
            var report = await _reportGenerator.GenerateReportAsync(periodStart, periodEnd, _schedule.Frequency);
            var success = await _emailSender.SendAsync(report);

            if (success)
            {
                _schedule.LastRunTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                CalculateAndSaveNextRunTime();

                ReportCompleted?.Invoke(this, new ReportEventArgs(true, DateTime.Now));
            }
            else
            {
                ReportCompleted?.Invoke(this, new ReportEventArgs(false, DateTime.Now, "Failed to send email"));
            }
        }
        catch (Exception ex)
        {
            ReportCompleted?.Invoke(this, new ReportEventArgs(false, DateTime.Now, ex.Message));
        }

        // Schedule next run
        ScheduleNextRun();
    }

    private void CalculateAndSaveNextRunTime()
    {
        if (_schedule == null)
            return;

        var now = DateTime.Now;
        var nextRun = now.Date.Add(_schedule.TimeOfDay);

        switch (_schedule.Frequency)
        {
            case ReportFrequency.Daily:
                if (nextRun <= now)
                    nextRun = nextRun.AddDays(1);
                break;

            case ReportFrequency.Weekly:
                var targetDayOfWeek = _schedule.DayOfWeek ?? DayOfWeek.Monday;
                var daysUntilTarget = ((int)targetDayOfWeek - (int)now.DayOfWeek + 7) % 7;
                if (daysUntilTarget == 0 && nextRun <= now)
                    daysUntilTarget = 7;
                nextRun = now.Date.AddDays(daysUntilTarget).Add(_schedule.TimeOfDay);
                break;

            case ReportFrequency.Monthly:
                var targetDay = Math.Min(_schedule.DayOfMonth ?? 1, DateTime.DaysInMonth(now.Year, now.Month));
                nextRun = new DateTime(now.Year, now.Month, targetDay).Add(_schedule.TimeOfDay);
                if (nextRun <= now)
                {
                    var nextMonth = now.AddMonths(1);
                    targetDay = Math.Min(_schedule.DayOfMonth ?? 1, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                    nextRun = new DateTime(nextMonth.Year, nextMonth.Month, targetDay).Add(_schedule.TimeOfDay);
                }
                break;
        }

        _schedule.NextRunTime = new DateTimeOffset(nextRun).ToUnixTimeSeconds();
        _settingsRepository.SaveSchedule(_schedule);
    }

    private static (DateTime start, DateTime end) GetReportPeriod(ReportFrequency frequency)
    {
        var now = DateTime.Now;
        var end = now;

        var start = frequency switch
        {
            ReportFrequency.Daily => now.AddDays(-1),
            ReportFrequency.Weekly => now.AddDays(-7),
            ReportFrequency.Monthly => now.AddMonths(-1),
            _ => now.AddDays(-1)
        };

        return (start, end);
    }
}
