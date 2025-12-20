using Microsoft.Data.Sqlite;

namespace DataUsageReporter.Data;

/// <summary>
/// Initializes SQLite database with WAL mode and creates schema.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = $"Data Source={databasePath}";
    }

    public string ConnectionString => _connectionString;

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable WAL mode for better concurrent access
        ExecuteCommand(connection, "PRAGMA journal_mode=WAL;");
        ExecuteCommand(connection, "PRAGMA synchronous=NORMAL;");

        // Create usage_records table
        ExecuteCommand(connection, @"
            CREATE TABLE IF NOT EXISTS usage_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp INTEGER NOT NULL,
                bytes_received INTEGER NOT NULL,
                bytes_sent INTEGER NOT NULL
            );");

        ExecuteCommand(connection, @"
            CREATE INDEX IF NOT EXISTS idx_usage_records_timestamp
            ON usage_records(timestamp);");

        // Create usage_summaries table
        ExecuteCommand(connection, @"
            CREATE TABLE IF NOT EXISTS usage_summaries (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                period_start INTEGER NOT NULL UNIQUE,
                period_end INTEGER NOT NULL,
                total_download INTEGER NOT NULL,
                total_upload INTEGER NOT NULL,
                peak_download_speed INTEGER NOT NULL,
                peak_upload_speed INTEGER NOT NULL,
                sample_count INTEGER NOT NULL
            );");

        ExecuteCommand(connection, @"
            CREATE INDEX IF NOT EXISTS idx_usage_summaries_period
            ON usage_summaries(period_start);");
    }

    private static void ExecuteCommand(SqliteConnection connection, string commandText)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }
}
