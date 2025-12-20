# Data Model: Filter Impossible Spike Values

**Feature**: 003-filter-spike-values
**Date**: 2025-12-20

## Overview

This feature modifies existing data flow without adding new entities. The primary change is adding filtering logic to the NetworkMonitor class and a configurable threshold to Settings.

## Existing Entities (No Changes)

### SpeedReading
Current speed measurement returned by NetworkMonitor.

| Field | Type | Description |
|-------|------|-------------|
| DownloadBytesPerSecond | long | Current download speed |
| UploadBytesPerSecond | long | Current upload speed |
| Timestamp | DateTime | When measurement was taken |

### UsageRecord
Stored in SQLite `usage_records` table.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| Timestamp | long | Unix epoch seconds |
| BytesReceived | long | Cumulative download bytes |
| BytesSent | long | Cumulative upload bytes |

### UsageSummary
Hourly aggregation stored in SQLite `usage_summaries` table.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| PeriodStart | long | Unix epoch seconds |
| PeriodEnd | long | Unix epoch seconds |
| TotalDownload | long | Total bytes downloaded |
| TotalUpload | long | Total bytes uploaded |
| PeakDownloadSpeed | long | Maximum download bytes/sec |
| PeakUploadSpeed | long | Maximum upload bytes/sec |
| SampleCount | int | Number of samples in period |

## Modified Entities

### Settings (Extended)
Add new property for spike threshold.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| MaxSpeedThresholdGbps | int | 10 | Maximum valid speed in Gbps |

**Validation Rules**:
- Minimum: 1 Gbps (reasonable minimum for any connection)
- Maximum: 100 Gbps (beyond current consumer technology)
- Stored as integer Gbps for simplicity

### NetworkMonitor (Extended State)
Add internal state for hold-last-value behavior.

| Field | Type | Description |
|-------|------|-------------|
| _lastValidSpeed | SpeedReading | Most recent reading that passed threshold check |
| _maxBytesPerSecond | long | Threshold in bytes/second (derived from settings) |

## Data Flow (Modified)

```text
GetIfTable2() → Raw counters
      ↓
NetworkMonitor.GetCurrentSpeed()
      ↓
[NEW] Threshold check: speed <= _maxBytesPerSecond?
      ├── YES → Return speed, update _lastValidSpeed
      └── NO  → Return _lastValidSpeed (hold behavior)
      ↓
SpeedReading (filtered)
      ↓
├── SpeedDisplay (UI) ← Shows filtered speed
├── UsageRepository.SaveRecord() ← Stores filtered data
└── UsageAggregator.AggregateHour() ← Uses filtered records
```

## State Transitions

### NetworkMonitor Speed Validation

```text
                    ┌─────────────────────┐
                    │   New Reading       │
                    │   Received          │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │  Speed <= Threshold? │
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              │ YES            │                │ NO
              ▼                │                ▼
    ┌─────────────────┐        │      ┌─────────────────┐
    │ Return Reading  │        │      │ Return Last     │
    │ Update Last     │        │      │ Valid Reading   │
    │ Valid           │        │      │ (Hold)          │
    └─────────────────┘        │      └─────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │   Reading Returned  │
                    │   to Caller         │
                    └─────────────────────┘
```

## Threshold Calculation

```text
User Setting: MaxSpeedThresholdGbps = 10

Conversion:
  10 Gbps = 10,000,000,000 bits/second
  10 Gbps = 1,250,000,000 bytes/second

Internal Constant:
  _maxBytesPerSecond = MaxSpeedThresholdGbps * 125_000_000L
```

## Database Impact

- **No schema changes required**
- **No data migration required**
- Existing spike data filtered at query time
- New data stored with filtering applied at collection time
