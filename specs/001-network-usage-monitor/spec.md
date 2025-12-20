# Feature Specification: Network Usage Monitor

**Feature Branch**: `001-network-usage-monitor`
**Created**: 2025-12-19
**Status**: Draft
**Input**: User description: "create a windows program to measure the data received and sent. a minimalist option panel allows to see a graph of usage by year, month, day, hour, minute, and schedule usage report by email. real-time usage (down and up) is displayed on the taskbar (refreshed every second)."

## Clarifications

### Session 2025-12-20

- Q: What visual format should the taskbar display use? → A: Dual line icon with download speed on top, upload below with directional arrows (stacked format)
- Q: How should VPN and firewall traffic be handled? → A: Measure all traffic at physical adapter level (includes VPN overhead)
- Q: How should SMTP credentials be stored securely? → A: Windows Credential Manager (OS-managed secure storage)
- Q: How should email reports work without SMTP configuration? → A: Use direct send mode (MX lookup to recipient server, no SMTP relay required)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Real-Time Taskbar Monitor (Priority: P1)

As a user, I want to see my current network upload and download speeds displayed directly on the Windows taskbar so I can monitor my network activity at a glance without opening any windows.

The application runs in the system tray and displays real-time download and upload speeds that update every second. The display is compact and unobtrusive, showing speeds in appropriate units (KB/s, MB/s, GB/s) based on current throughput.

**Why this priority**: This is the core value proposition - instant visibility into network activity without any user interaction required. It delivers immediate, continuous value from the moment the application starts.

**Independent Test**: Can be fully tested by launching the application and observing that taskbar displays update every second with accurate network speed readings. Delivers immediate monitoring value.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** I look at the taskbar/system tray, **Then** I see current download and upload speeds displayed and updating every second
2. **Given** the application is running, **When** network activity occurs, **Then** the displayed speeds accurately reflect the actual throughput
3. **Given** the application is running with no network activity, **When** I look at the taskbar, **Then** I see 0 KB/s or similar indication of no activity
4. **Given** high network throughput (e.g., 100+ MB/s), **When** I look at the taskbar, **Then** speeds are displayed in appropriate units (MB/s or GB/s) for readability

---

### User Story 2 - Historical Usage Graphs (Priority: P2)

As a user, I want to view my network usage history through graphs organized by different time periods (year, month, day, hour, minute) so I can understand my data consumption patterns and identify unusual usage.

The options panel provides a graph view where users can select a time granularity and see their accumulated data usage. Each graph shows both download and upload volumes for the selected period.

**Why this priority**: Historical data provides context and insights that complement real-time monitoring. Users can identify trends, troubleshoot past events, and manage their data consumption.

**Independent Test**: Can be tested by running the application for a period, opening the options panel, selecting different time views, and verifying graphs display accurate historical data.

**Acceptance Scenarios**:

1. **Given** the application has been running and collecting data, **When** I open the options panel and select "by day" view, **Then** I see a graph showing daily download and upload totals
2. **Given** historical data exists, **When** I switch between year/month/day/hour/minute views, **Then** the graph updates to show data at the selected granularity
3. **Given** the graph is displayed, **When** I hover over or click a data point, **Then** I see the exact values for that period
4. **Given** no historical data exists for a period, **When** I view that period, **Then** the graph shows zero values or an empty state message

---

### User Story 3 - Scheduled Email Reports (Priority: P3)

As a user, I want to schedule automatic email reports of my network usage so I can receive regular summaries without manually checking the application.

The options panel allows users to configure their email settings (SMTP server, credentials) and schedule when reports should be sent (daily, weekly, monthly). Reports include usage summaries and key statistics for the reporting period.

**Why this priority**: Email reports provide passive monitoring for users who want periodic summaries without active engagement. This builds on top of the data collection from P1 and visualization from P2.

**Independent Test**: Can be tested by configuring email settings, scheduling a report, and verifying the report arrives at the scheduled time with accurate usage data.

**Acceptance Scenarios**:

1. **Given** I am in the options panel, **When** I configure SMTP settings and schedule, **Then** the settings are saved and validated
2. **Given** email is configured and a schedule is set, **When** the scheduled time arrives, **Then** a usage report is sent to the configured email address
3. **Given** a report is sent, **When** I receive it, **Then** it contains accurate usage statistics for the reporting period
4. **Given** email settings are invalid, **When** I try to save them, **Then** I receive a clear error message explaining the problem

---

### Edge Cases

- What happens when network interfaces are disconnected/reconnected?
  - Application continues running and resumes monitoring when connectivity returns
- What happens when the system wakes from sleep/hibernation?
  - Application resumes monitoring; does not count sleep time as usage gap
- What happens when disk storage for historical data is full?
  - Oldest data is automatically pruned to make room for new data
- What happens when email sending fails?
  - Failure is logged; retry occurs at next scheduled interval; user is notified via system notification
- What happens when direct send mode is blocked (ISP blocks port 25)?
  - User is notified to configure SMTP relay as fallback; option to use alternative port or SMTP provided
- What happens when multiple network interfaces are active?
  - All active interfaces are monitored and combined into a single total
- What happens when the application starts with Windows?
  - Application launches minimized to system tray and immediately begins monitoring

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST measure and record bytes received and sent across all active network interfaces
- **FR-002**: System MUST display real-time download and upload speeds in the Windows system tray using a dual-line stacked format (download on top with ↓ arrow, upload below with ↑ arrow)
- **FR-003**: System MUST update the taskbar display every second
- **FR-004**: System MUST automatically select appropriate speed units (B/s, KB/s, MB/s, GB/s) based on throughput
- **FR-005**: System MUST persist usage data locally for historical tracking
- **FR-006**: System MUST provide a minimalist options panel accessible from the system tray icon
- **FR-007**: System MUST display historical usage graphs with selectable time granularity (year, month, day, hour, minute)
- **FR-008**: System MUST show both download and upload volumes separately in graphs
- **FR-009**: System MUST support email report delivery via direct send mode (MX lookup) by default, with optional SMTP relay configuration; SMTP credentials stored securely in Windows Credential Manager when configured
- **FR-010**: System MUST allow users to schedule usage reports (daily, weekly, or monthly)
- **FR-011**: System MUST send scheduled reports containing usage summaries for the reporting period
- **FR-012**: System MUST start automatically with Windows (configurable)
- **FR-013**: System MUST run without requiring administrator privileges
- **FR-014**: System MUST retain historical data for at least 1 year (rolling window)

### Key Entities

- **Usage Record**: A timestamped measurement of bytes downloaded and uploaded; includes timestamp, bytes received, bytes sent, and measurement interval
- **Usage Summary**: Aggregated usage data for a time period; includes period start/end, total download, total upload, peak speeds
- **Email Configuration**: User's SMTP settings for report delivery; includes server address, port, authentication credentials, recipient address
- **Report Schedule**: Configuration for automated report delivery; includes frequency (daily/weekly/monthly), time of day, enabled status

## Assumptions

- Users need only a recipient email address to use the email reporting feature; SMTP relay configuration is optional (direct send mode available by default)
- The Windows system has at least one network interface to monitor
- Historical data is stored in the user's AppData folder (no admin rights needed)
- Data retention of 1 year provides sufficient history for most use cases
- Combining all network interfaces into a single total is the expected default behavior
- Traffic is measured at the physical adapter level, which includes VPN tunnel overhead (represents actual bandwidth consumption)
- The taskbar display uses the system tray notification area

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Real-time speed display updates every second with less than 100ms variance
- **SC-002**: Application uses less than 50MB of memory during normal operation
- **SC-003**: Application uses less than 5% CPU during idle monitoring periods
- **SC-004**: Application starts and displays speeds within 2 seconds of launch
- **SC-005**: Historical data queries return results within 1 second for any time range
- **SC-006**: Users can configure email and schedule reports in under 2 minutes
- **SC-007**: Scheduled email reports are delivered within 5 minutes of scheduled time
- **SC-008**: Graph views render within 500ms when switching time granularities
- **SC-009**: Application runs continuously for 30+ days without memory leaks or crashes
- **SC-010**: Data storage grows by less than 10MB per month of usage history
