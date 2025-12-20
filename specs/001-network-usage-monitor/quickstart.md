# Quickstart: Network Usage Monitor

**Date**: 2025-12-20
**Feature**: 001-network-usage-monitor

## Prerequisites

- Windows 10/11 (64-bit)
- .NET 8.0 SDK (for development only; runtime bundled in release)
- Visual Studio 2022 or VS Code with C# extension

## Project Setup

### 1. Create Solution

```powershell
# Create solution and projects
dotnet new sln -n DataUsageReporter
dotnet new winforms -n DataUsageReporter -f net8.0-windows
dotnet new xunit -n DataUsageReporter.Tests -f net8.0

# Add projects to solution
dotnet sln add src/DataUsageReporter/DataUsageReporter.csproj
dotnet sln add tests/DataUsageReporter.Tests/DataUsageReporter.Tests.csproj

# Add project reference
dotnet add tests/DataUsageReporter.Tests reference src/DataUsageReporter
```

### 2. Install Dependencies

```powershell
cd src/DataUsageReporter

# Network statistics
dotnet add package Vanara.PInvoke.IpHlpApi

# Database
dotnet add package Microsoft.Data.Sqlite

# Charting
dotnet add package ScottPlot.WinForms

# Email
dotnet add package MailKit

cd ../tests/DataUsageReporter.Tests

# Testing
dotnet add package Moq
dotnet add package FluentAssertions
```

### 3. Configure Project

Edit `src/DataUsageReporter/DataUsageReporter.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationIcon>Resources\app.ico</ApplicationIcon>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Single file deployment -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>partial</TrimMode>
  </PropertyGroup>
</Project>
```

### 4. Create Directory Structure

```powershell
cd src/DataUsageReporter
mkdir Core, Data, Data/Migrations, Email, UI, Resources
```

## Development Workflow

### Build

```powershell
dotnet build
```

### Run

```powershell
dotnet run --project src/DataUsageReporter
```

### Test

```powershell
dotnet test
```

### Publish (Single Executable)

```powershell
dotnet publish src/DataUsageReporter -c Release -o ./publish
```

Output: `./publish/DataUsageReporter.exe` (~15-25MB)

## Key Implementation Patterns

### 1. Network Monitoring Loop

```csharp
// Program.cs - Main loop setup
var timer = new System.Windows.Forms.Timer { Interval = 1000 };
timer.Tick += (s, e) => {
    var speed = networkMonitor.GetCurrentSpeed();
    trayIcon.UpdateSpeed(speed);
    repository.SaveRecordAsync(new UsageRecord(
        DateTime.UtcNow,
        speed.TotalBytesReceived,
        speed.TotalBytesSent
    ));
};
timer.Start();
```

### 2. System Tray Setup

```csharp
// UI/TrayIcon.cs
public class TrayIcon : ITrayIcon
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = Resources.AppIcon,
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };
        _notifyIcon.DoubleClick += (s, e) => OptionsRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSpeed(SpeedReading speed)
    {
        _notifyIcon.Text = $"↓ {FormatSpeed(speed.Download)}\n↑ {FormatSpeed(speed.Upload)}";
    }
}
```

### 3. SQLite Repository

```csharp
// Data/UsageRepository.cs
public class UsageRepository : IUsageRepository
{
    private readonly string _connectionString;

    public UsageRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable WAL mode
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();

        // Create tables...
    }
}
```

### 4. Credential Manager

```csharp
// Data/CredentialManager.cs
using System.Runtime.InteropServices;

public class CredentialManager : ICredentialManager
{
    private const string TargetPrefix = "DataUsageReporter:";

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    // P/Invoke definitions for CredRead, CredDelete...
}
```

## Verification Checklist

After implementation, verify these success criteria:

| Criterion | How to Verify |
|-----------|---------------|
| SC-001: 1-second updates | Observe taskbar updates with stopwatch |
| SC-002: <50MB memory | Task Manager while running |
| SC-003: <5% CPU idle | Task Manager over 1 minute |
| SC-004: <2s startup | Measure with Stopwatch in code |
| SC-005: <1s queries | Profile GetSummariesAsync with test data |
| SC-008: <500ms graphs | Profile graph render time |

## Common Issues

### Issue: Access Denied to Network Statistics

**Cause**: Running as restricted user
**Solution**: IP Helper API doesn't require admin for reading. Check if antivirus is blocking.

### Issue: Database Locked

**Cause**: Multiple instances or crash during write
**Solution**: Use `PRAGMA journal_mode=WAL` and ensure single instance check in Program.cs.

### Issue: Email Send Fails

**Cause**: SMTP authentication or TLS issues
**Solution**: Test with `TestConnectionAsync()` and check port (587 for STARTTLS, 465 for SSL).

## Next Steps

1. Run `/speckit.tasks` to generate implementation tasks
2. Implement P1 (Real-Time Monitor) first as MVP
3. Add P2 (Historical Graphs) after P1 is stable
4. Add P3 (Email Reports) last
