using Microsoft.Data.Sqlite;
using DataUsageReporter.Data.Models;

namespace DataUsageReporter.Data;

/// <summary>
/// SQLite-based repository for usage records and summaries.
/// Uses WAL mode for concurrent read/write access.
/// </summary>
public class UsageRepository : IUsageRepository
{
    private readonly string _connectionString;

    public UsageRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveRecordAsync(UsageRecord record)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO usage_records (timestamp, bytes_received, bytes_sent)
            VALUES (@timestamp, @bytesReceived, @bytesSent)";
        command.Parameters.AddWithValue("@timestamp", record.Timestamp);
        command.Parameters.AddWithValue("@bytesReceived", record.BytesReceived);
        command.Parameters.AddWithValue("@bytesSent", record.BytesSent);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<UsageRecord>> GetRecordsSinceAsync(DateTime since)
    {
        var sinceUnix = new DateTimeOffset(since.ToUniversalTime()).ToUnixTimeSeconds();
        var records = new List<UsageRecord>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, timestamp, bytes_received, bytes_sent
            FROM usage_records
            WHERE timestamp >= @since
            ORDER BY timestamp ASC";
        command.Parameters.AddWithValue("@since", sinceUnix);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new UsageRecord
            {
                Id = reader.GetInt64(0),
                Timestamp = reader.GetInt64(1),
                BytesReceived = reader.GetInt64(2),
                BytesSent = reader.GetInt64(3)
            });
        }

        return records;
    }

    public async Task DeleteRecordsBeforeAsync(DateTime before)
    {
        var beforeUnix = new DateTimeOffset(before.ToUniversalTime()).ToUnixTimeSeconds();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM usage_records WHERE timestamp < @before";
        command.Parameters.AddWithValue("@before", beforeUnix);

        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveSummaryAsync(UsageSummary summary)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO usage_summaries
            (period_start, period_end, total_download, total_upload,
             peak_download_speed, peak_upload_speed, sample_count)
            VALUES (@periodStart, @periodEnd, @totalDownload, @totalUpload,
                    @peakDownloadSpeed, @peakUploadSpeed, @sampleCount)";
        command.Parameters.AddWithValue("@periodStart", summary.PeriodStart);
        command.Parameters.AddWithValue("@periodEnd", summary.PeriodEnd);
        command.Parameters.AddWithValue("@totalDownload", summary.TotalDownload);
        command.Parameters.AddWithValue("@totalUpload", summary.TotalUpload);
        command.Parameters.AddWithValue("@peakDownloadSpeed", summary.PeakDownloadSpeed);
        command.Parameters.AddWithValue("@peakUploadSpeed", summary.PeakUploadSpeed);
        command.Parameters.AddWithValue("@sampleCount", summary.SampleCount);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<UsageSummary>> GetSummariesAsync(DateTime from, DateTime to)
    {
        var fromUnix = new DateTimeOffset(from.ToUniversalTime()).ToUnixTimeSeconds();
        var toUnix = new DateTimeOffset(to.ToUniversalTime()).ToUnixTimeSeconds();
        var summaries = new List<UsageSummary>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, period_start, period_end, total_download, total_upload,
                   peak_download_speed, peak_upload_speed, sample_count
            FROM usage_summaries
            WHERE period_start >= @from AND period_start < @to
            ORDER BY period_start ASC";
        command.Parameters.AddWithValue("@from", fromUnix);
        command.Parameters.AddWithValue("@to", toUnix);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            summaries.Add(new UsageSummary
            {
                Id = reader.GetInt64(0),
                PeriodStart = reader.GetInt64(1),
                PeriodEnd = reader.GetInt64(2),
                TotalDownload = reader.GetInt64(3),
                TotalUpload = reader.GetInt64(4),
                PeakDownloadSpeed = reader.GetInt64(5),
                PeakUploadSpeed = reader.GetInt64(6),
                SampleCount = reader.GetInt32(7)
            });
        }

        return summaries;
    }

    public async Task DeleteSummariesBeforeAsync(DateTime before)
    {
        var beforeUnix = new DateTimeOffset(before.ToUniversalTime()).ToUnixTimeSeconds();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM usage_summaries WHERE period_start < @before";
        command.Parameters.AddWithValue("@before", beforeUnix);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<UsageSummary?> GetTotalUsageAsync(DateTime from, DateTime to)
    {
        var fromUnix = new DateTimeOffset(from.ToUniversalTime()).ToUnixTimeSeconds();
        var toUnix = new DateTimeOffset(to.ToUniversalTime()).ToUnixTimeSeconds();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                MIN(period_start) as period_start,
                MAX(period_end) as period_end,
                SUM(total_download) as total_download,
                SUM(total_upload) as total_upload,
                MAX(peak_download_speed) as peak_download_speed,
                MAX(peak_upload_speed) as peak_upload_speed,
                SUM(sample_count) as sample_count
            FROM usage_summaries
            WHERE period_start >= @from AND period_start < @to";
        command.Parameters.AddWithValue("@from", fromUnix);
        command.Parameters.AddWithValue("@to", toUnix);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync() && !reader.IsDBNull(0))
        {
            return new UsageSummary
            {
                PeriodStart = reader.GetInt64(0),
                PeriodEnd = reader.GetInt64(1),
                TotalDownload = reader.GetInt64(2),
                TotalUpload = reader.GetInt64(3),
                PeakDownloadSpeed = reader.GetInt64(4),
                PeakUploadSpeed = reader.GetInt64(5),
                SampleCount = reader.GetInt32(6)
            };
        }

        return null;
    }

    public async Task<UsageSummary?> CalculateUsageFromRecordsAsync(DateTime from, DateTime to)
    {
        var fromUnix = new DateTimeOffset(from.ToUniversalTime()).ToUnixTimeSeconds();
        var toUnix = new DateTimeOffset(to.ToUniversalTime()).ToUnixTimeSeconds();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Get all records in range, ordered by timestamp
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT timestamp, bytes_received, bytes_sent
            FROM usage_records
            WHERE timestamp >= @from AND timestamp < @to
            ORDER BY timestamp ASC";
        command.Parameters.AddWithValue("@from", fromUnix);
        command.Parameters.AddWithValue("@to", toUnix);

        var records = new List<(long Timestamp, long BytesReceived, long BytesSent)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add((reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2)));
        }

        if (records.Count < 2)
            return null;

        // Calculate deltas between consecutive readings
        long totalDownload = 0;
        long totalUpload = 0;
        long peakDownloadSpeed = 0;
        long peakUploadSpeed = 0;

        for (int i = 1; i < records.Count; i++)
        {
            var prev = records[i - 1];
            var curr = records[i];

            var timeDiff = curr.Timestamp - prev.Timestamp;
            if (timeDiff <= 0 || timeDiff > 10) continue; // Skip if gap > 10 seconds (likely restart)

            // Calculate byte deltas (handle counter resets)
            var downloadDelta = curr.BytesReceived >= prev.BytesReceived
                ? curr.BytesReceived - prev.BytesReceived
                : curr.BytesReceived; // Counter reset

            var uploadDelta = curr.BytesSent >= prev.BytesSent
                ? curr.BytesSent - prev.BytesSent
                : curr.BytesSent; // Counter reset

            // Skip unreasonably large values (likely counter reset or multiple adapters)
            if (downloadDelta > 1_000_000_000 || uploadDelta > 1_000_000_000) continue;

            totalDownload += downloadDelta;
            totalUpload += uploadDelta;

            // Calculate speed (bytes per second)
            var downloadSpeed = downloadDelta / timeDiff;
            var uploadSpeed = uploadDelta / timeDiff;

            if (downloadSpeed > peakDownloadSpeed) peakDownloadSpeed = downloadSpeed;
            if (uploadSpeed > peakUploadSpeed) peakUploadSpeed = uploadSpeed;
        }

        return new UsageSummary
        {
            PeriodStart = fromUnix,
            PeriodEnd = toUnix,
            TotalDownload = totalDownload,
            TotalUpload = totalUpload,
            PeakDownloadSpeed = peakDownloadSpeed,
            PeakUploadSpeed = peakUploadSpeed,
            SampleCount = records.Count
        };
    }

    /// <summary>
    /// Seeds test data for the past N weeks. For testing purposes only.
    /// </summary>
    public async Task SeedTestDataAsync(int weeks = 10)
    {
        var random = new Random(42); // Fixed seed for reproducible data
        var now = DateTime.Now;
        var startDate = now.AddDays(-weeks * 7);

        // Start from the beginning of that hour
        var currentHour = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, 0, 0);
        var endHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        while (currentHour < endHour)
        {
            var periodStart = new DateTimeOffset(currentHour.ToUniversalTime()).ToUnixTimeSeconds();
            var periodEnd = new DateTimeOffset(currentHour.AddHours(1).ToUniversalTime()).ToUnixTimeSeconds();

            // Generate realistic usage data
            // Base usage varies by hour of day (more during day, less at night)
            var hourOfDay = currentHour.Hour;
            var dayMultiplier = hourOfDay >= 8 && hourOfDay <= 22 ? 1.0 : 0.3; // Less usage at night
            var weekendMultiplier = currentHour.DayOfWeek == DayOfWeek.Saturday || currentHour.DayOfWeek == DayOfWeek.Sunday ? 1.5 : 1.0;

            // Random variation
            var variation = 0.5 + random.NextDouble(); // 0.5 to 1.5

            // Base: ~100 MB/hour download, ~20 MB/hour upload
            var baseDownload = 100_000_000L; // 100 MB
            var baseUpload = 20_000_000L;    // 20 MB

            var totalDownload = (long)(baseDownload * dayMultiplier * weekendMultiplier * variation);
            var totalUpload = (long)(baseUpload * dayMultiplier * weekendMultiplier * variation * 0.8);

            // Peak speeds (bytes/sec) - up to 50 Mbps download, 10 Mbps upload
            var peakDownloadSpeed = (long)(random.NextDouble() * 6_250_000); // Up to 50 Mbps
            var peakUploadSpeed = (long)(random.NextDouble() * 1_250_000);   // Up to 10 Mbps

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO usage_summaries
                (period_start, period_end, total_download, total_upload,
                 peak_download_speed, peak_upload_speed, sample_count)
                VALUES (@periodStart, @periodEnd, @totalDownload, @totalUpload,
                        @peakDownloadSpeed, @peakUploadSpeed, @sampleCount)";
            command.Parameters.AddWithValue("@periodStart", periodStart);
            command.Parameters.AddWithValue("@periodEnd", periodEnd);
            command.Parameters.AddWithValue("@totalDownload", totalDownload);
            command.Parameters.AddWithValue("@totalUpload", totalUpload);
            command.Parameters.AddWithValue("@peakDownloadSpeed", peakDownloadSpeed);
            command.Parameters.AddWithValue("@peakUploadSpeed", peakUploadSpeed);
            command.Parameters.AddWithValue("@sampleCount", 3600); // 1 sample per second

            await command.ExecuteNonQueryAsync();

            currentHour = currentHour.AddHours(1);
        }
    }
}
