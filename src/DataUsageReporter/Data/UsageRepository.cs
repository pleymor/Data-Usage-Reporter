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
}
