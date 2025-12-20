# Research: Network Usage Monitor

**Date**: 2025-12-20
**Feature**: 001-network-usage-monitor

## Technology Decisions

### 1. Language & Framework

**Decision**: C# .NET 8.0 with WinForms

**Rationale**:
- Native Windows integration with built-in `NotifyIcon` for system tray
- Single-file deployment via `PublishSingleFile` + trimming (~15-25MB)
- Startup time <500ms meets constitution requirement (<2s)
- Memory footprint ~20-40MB meets spec requirement (<50MB)
- Direct P/Invoke access to Windows APIs without external dependencies

**Alternatives Considered**:
| Option | Pros | Cons | Rejected Because |
|--------|------|------|------------------|
| Go + Wails | Small binary (~4MB), fast | CGO required for systray, web-based UI | Less native Windows feel, CGO complexity |
| Rust + egui | Very fast, small binary | Steeper learning curve, less Windows-native | Development velocity, Windows API access complexity |
| Python + PyQt | Rapid development | Large runtime, slow startup, packaging complexity | Constitution violation: not lightweight, startup >2s |

### 2. Network Statistics API

**Decision**: IP Helper API via Vanara.PInvoke.IpHlpApi

**Rationale**:
- `GetIfTable2()` and `GetIfEntry2()` provide direct access to `InOctets`/`OutOctets`
- No administrator privileges required for reading network statistics
- Lower overhead than Performance Counters (<1% CPU at 1-second polling)
- Pre-built, tested P/Invoke wrappers from Vanara package

**Alternatives Considered**:
| Option | Pros | Cons | Rejected Because |
|--------|------|------|------------------|
| Performance Counters | Simpler .NET API | Higher overhead, requires initialization | Constitution: resource efficiency |
| WMI | High-level queries | Slow, high overhead | Performance not suitable for 1-second updates |
| Raw sockets | Maximum control | Requires admin, complex | Constitution: no admin privileges |

**Key API Functions**:
- `GetIfTable2()` - retrieves all network interface statistics
- `MIB_IF_ROW2.InOctets` - total bytes received
- `MIB_IF_ROW2.OutOctets` - total bytes sent

### 3. Local Storage

**Decision**: SQLite with Microsoft.Data.Sqlite

**Rationale**:
- Efficient time-series queries with SQL and indexing
- WAL mode for concurrent reads during writes
- Automatic retention via `DELETE WHERE timestamp < ...`
- Hourly aggregation keeps storage <10MB/month (438KB/year per interface)
- In-memory mode available for unit testing

**Alternatives Considered**:
| Option | Pros | Cons | Rejected Because |
|--------|------|------|------------------|
| LiteDB | Pure C#, NoSQL | Single-threaded, less query power | Time-series queries less efficient |
| Flat files (JSON/CSV) | Simplest | No query capability, manual retention | Complexity for aggregation queries |
| Embedded RocksDB | Fast writes | Large binary, overkill | Constitution: simplicity |

**Configuration**:
```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA auto_vacuum = INCREMENTAL;
```

### 4. Charting Library

**Decision**: ScottPlot.WinForms

**Rationale**:
- Designed for scientific/time-series data with large datasets
- Native WinForms integration
- No animations by default (saves CPU during idle)
- Efficient rendering (<500ms for graph updates)
- Active development, good documentation

**Alternatives Considered**:
| Option | Pros | Cons | Rejected Because |
|--------|------|------|------------------|
| LiveCharts | Pretty animations | Higher memory, CPU for animations | Constitution: resource efficiency |
| OxyPlot | Lightweight | Less active development | ScottPlot has better time-series support |
| MS Chart Controls | Built-in | Limited features, dated | Poor large dataset handling |

### 5. Email Library

**Decision**: MailKit

**Rationale**:
- Microsoft-recommended replacement for obsolete `SmtpClient`
- Modern authentication support (OAuth2, XOAUTH2)
- Async operations for non-blocking sends
- Lightweight, well-maintained

**Alternatives Considered**:
| Option | Pros | Cons | Rejected Because |
|--------|------|------|------------------|
| System.Net.Mail.SmtpClient | Built-in | Deprecated, missing modern auth | Microsoft recommends MailKit |
| FluentEmail | Fluent API | Additional abstraction layer | Constitution: simplicity |

### 6. Credential Storage

**Decision**: Windows Credential Manager

**Rationale**:
- OS-managed secure storage (DPAPI encryption)
- No admin rights required
- Standard Windows pattern for credential storage
- User expectation for Windows applications

**Implementation**: Use `CredentialManagement` NuGet package or direct P/Invoke to `Advapi32.dll`

### 7. Deployment Strategy

**Decision**: Single-file self-contained with trimming

**Configuration**:
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>partial</TrimMode>
```

**Expected Output**:
- File size: ~15-25MB
- Startup time: <500ms
- No .NET runtime installation required

**Note**: Native AOT was considered for faster startup (~17ms) but WinForms trimming compatibility issues make standard single-file safer.

## Package Summary

| Package | Version | Purpose |
|---------|---------|---------|
| Vanara.PInvoke.IpHlpApi | 4.x | Network statistics via IP Helper API |
| Microsoft.Data.Sqlite | 8.x | SQLite database access |
| ScottPlot.WinForms | 5.x | Usage graphs and charts |
| MailKit | 4.x | SMTP email sending |
| xUnit | 2.x | Unit testing framework |
| Moq | 4.x | Mocking for tests |

## Risk Mitigations

| Risk | Mitigation |
|------|------------|
| VPN traffic double-counting | Measure at physical adapter level only (confirmed in spec clarifications) |
| High CPU from 1-second polling | IP Helper API tested at <1% CPU; use efficient delta calculation |
| Memory growth from graph data | Limit in-memory data points; aggregate older data |
| SQLite file corruption | WAL mode + periodic checkpoints; backup strategy |
| Email failures | Retry at next interval; notify user via system notification |
