# Data Model: Network Usage Monitor

**Date**: 2025-12-20
**Feature**: 001-network-usage-monitor

## Entity Overview

```
┌─────────────────┐       ┌─────────────────┐
│  UsageRecord    │       │  UsageSummary   │
│  (raw samples)  │──────>│  (aggregated)   │
└─────────────────┘       └─────────────────┘
                                   │
                                   v
┌─────────────────┐       ┌─────────────────┐
│ EmailConfig     │       │ ReportSchedule  │
└─────────────────┘<──────┤                 │
                          └─────────────────┘
```

## Entities

### 1. UsageRecord

Raw network usage samples collected every second. Stored temporarily and aggregated into UsageSummary records.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int64 | PK, auto-increment | Unique identifier |
| Timestamp | int64 | NOT NULL, indexed | Unix epoch (seconds) |
| BytesReceived | int64 | NOT NULL, >= 0 | Cumulative bytes received at sample time |
| BytesSent | int64 | NOT NULL, >= 0 | Cumulative bytes sent at sample time |

**Retention**: Raw records kept for 1 hour, then deleted after aggregation.

**SQLite Schema**:
```sql
CREATE TABLE usage_records (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp INTEGER NOT NULL,
    bytes_received INTEGER NOT NULL,
    bytes_sent INTEGER NOT NULL
);
CREATE INDEX idx_usage_records_timestamp ON usage_records(timestamp);
```

### 2. UsageSummary

Aggregated usage data for historical tracking. One record per hour.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int64 | PK, auto-increment | Unique identifier |
| PeriodStart | int64 | NOT NULL, indexed, unique | Unix epoch of hour start |
| PeriodEnd | int64 | NOT NULL | Unix epoch of hour end |
| TotalDownload | int64 | NOT NULL, >= 0 | Total bytes downloaded in period |
| TotalUpload | int64 | NOT NULL, >= 0 | Total bytes uploaded in period |
| PeakDownloadSpeed | int64 | NOT NULL, >= 0 | Peak download speed (bytes/sec) |
| PeakUploadSpeed | int64 | NOT NULL, >= 0 | Peak upload speed (bytes/sec) |
| SampleCount | int | NOT NULL, > 0 | Number of samples aggregated |

**Retention**: 1 year rolling window (8,760 records max per year).

**Storage Estimate**: ~50 bytes/record × 8,760 records = ~430 KB/year

**SQLite Schema**:
```sql
CREATE TABLE usage_summaries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    period_start INTEGER NOT NULL UNIQUE,
    period_end INTEGER NOT NULL,
    total_download INTEGER NOT NULL,
    total_upload INTEGER NOT NULL,
    peak_download_speed INTEGER NOT NULL,
    peak_upload_speed INTEGER NOT NULL,
    sample_count INTEGER NOT NULL
);
CREATE INDEX idx_usage_summaries_period ON usage_summaries(period_start);
```

### 3. EmailConfig

User's SMTP configuration for email reports. Stored in settings, credentials in Windows Credential Manager.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| SmtpServer | string | NOT NULL, max 255 | SMTP server hostname |
| SmtpPort | int | NOT NULL, 1-65535 | SMTP port (typically 587 or 465) |
| UseSsl | bool | NOT NULL | Use SSL/TLS connection |
| SenderEmail | string | NOT NULL, valid email | From address |
| RecipientEmail | string | NOT NULL, valid email | To address |
| Username | string | max 255 | SMTP username (stored in Credential Manager) |
| CredentialKey | string | NOT NULL | Reference key for Credential Manager |

**Validation Rules**:
- SmtpServer: Valid hostname or IP address
- SmtpPort: Common ports 25, 465, 587, 2525
- SenderEmail/RecipientEmail: RFC 5322 email format
- Credentials stored securely in Windows Credential Manager, not in config file

**Storage**: JSON in %AppData%\DataUsageReporter\settings.json (excluding credentials)

### 4. ReportSchedule

Configuration for automated email report delivery.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | PK | Unique identifier |
| Frequency | enum | NOT NULL | Daily, Weekly, Monthly |
| TimeOfDay | TimeSpan | NOT NULL | Hour:Minute to send (24h format) |
| DayOfWeek | int? | 0-6, nullable | Day for weekly reports (0=Sunday) |
| DayOfMonth | int? | 1-28, nullable | Day for monthly reports |
| IsEnabled | bool | NOT NULL | Whether schedule is active |
| LastRunTime | int64? | nullable | Unix epoch of last successful send |
| NextRunTime | int64 | NOT NULL | Unix epoch of next scheduled send |

**State Transitions**:
```
[Created] -> [Enabled] -> [Running] -> [Completed] -> [Scheduled]
                 │             │
                 v             v
            [Disabled]     [Failed] -> [Retry]
```

**Validation Rules**:
- DayOfWeek required if Frequency = Weekly
- DayOfMonth required if Frequency = Monthly
- DayOfMonth capped at 28 to handle all months

**Storage**: JSON in %AppData%\DataUsageReporter\settings.json

### 5. AppSettings

Application-wide settings (non-entity, configuration object).

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| StartWithWindows | bool | true | Launch on Windows startup |
| DataRetentionDays | int | 365 | Days to retain historical data |
| UpdateIntervalMs | int | 1000 | Real-time display update interval |
| DatabasePath | string | %AppData%\...\usage.db | SQLite database location |

**Storage**: JSON in %AppData%\DataUsageReporter\settings.json

## Aggregation Logic

### Real-time to Hourly Aggregation

```
Every hour (on the hour):
1. Query all UsageRecords from the past hour
2. Calculate:
   - TotalDownload = last.BytesReceived - first.BytesReceived
   - TotalUpload = last.BytesSent - first.BytesSent
   - PeakDownloadSpeed = max(delta_received / interval)
   - PeakUploadSpeed = max(delta_sent / interval)
3. Insert UsageSummary record
4. Delete processed UsageRecords
```

### Query Patterns for Graphs

| View | Query |
|------|-------|
| By Minute | Last 60 UsageRecords, calculate deltas |
| By Hour | Last 24 UsageSummary records |
| By Day | Aggregate 24 UsageSummary records per day |
| By Month | Aggregate UsageSummary by month |
| By Year | Aggregate UsageSummary by year |

## Data Retention

```sql
-- Run daily to prune old data
DELETE FROM usage_summaries
WHERE period_start < strftime('%s', 'now', '-365 days');

-- Run hourly after aggregation
DELETE FROM usage_records
WHERE timestamp < strftime('%s', 'now', '-1 hour');

-- Reclaim space periodically
PRAGMA incremental_vacuum;
```

## File Locations

| File | Location | Purpose |
|------|----------|---------|
| usage.db | %AppData%\DataUsageReporter\ | SQLite database |
| settings.json | %AppData%\DataUsageReporter\ | App configuration |
| DataUsageReporter | Windows Credential Manager | SMTP credentials |
