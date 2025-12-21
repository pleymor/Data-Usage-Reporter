# Feature Specification: Enhanced Email Reports with Graphs and Tables

**Feature Branch**: `007-enhanced-email-reports`
**Created**: 2025-12-21
**Status**: Draft
**Input**: User description: "Improve the network usage report to present more information, with graphs and tables, adapted to the chosen frequency. Daily: hour-by-hour graph followed by a table of the last 7 days with 1 row per day (dl in MB, ul in MB, dl peak and ul peak in Mbps). Weekly: day-by-day graph of the last 7 days followed by a table of the last 31 days (same structure): 1 row per day. Monthly: week-by-week graph of the last 5 weeks followed by a table of the last 31 days (same structure)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Daily Report with Hourly Graph (Priority: P1)

A user subscribing to daily email reports receives an enhanced report containing a visual graph showing network usage hour-by-hour for the current day, followed by a summary table displaying the last 7 days of data with download (MB), upload (MB), peak download speed (Mbps), and peak upload speed (Mbps) per day.

**Why this priority**: Daily reports are the most commonly used frequency and provide users with immediate, granular visibility into their network usage patterns throughout the day.

**Independent Test**: Can be fully tested by configuring a daily report schedule, triggering the report, and verifying the email contains an hourly graph and 7-day table with correct data.

**Acceptance Scenarios**:

1. **Given** a user has daily reports enabled, **When** the daily report is sent, **Then** the email contains an hourly graph (24 data points from midnight to 11 PM) showing download and upload totals (MB)
2. **Given** a user receives a daily report, **When** viewing the email, **Then** a table displays the last 7 days with columns: Date, Download (MB), Upload (MB), Peak Download (Mbps), Peak Upload (Mbps)
3. **Given** a day has no recorded data, **When** included in the 7-day table, **Then** that day shows zero values rather than being omitted

---

### User Story 2 - Weekly Report with Daily Graph (Priority: P2)

A user subscribing to weekly email reports receives an enhanced report containing a visual graph showing network usage day-by-day for the last 7 days, followed by a summary table displaying the last 31 days of data with the same structure as the daily report table.

**Why this priority**: Weekly reports provide a broader view for users who don't need daily monitoring but want regular summaries of their usage trends.

**Independent Test**: Can be fully tested by configuring a weekly report schedule, triggering the report, and verifying the email contains a 7-day graph and 31-day table with correct data.

**Acceptance Scenarios**:

1. **Given** a user has weekly reports enabled, **When** the weekly report is sent, **Then** the email contains a daily graph (7 data points) showing download and upload totals per day
2. **Given** a user receives a weekly report, **When** viewing the email, **Then** a table displays the last 31 days with columns: Date, Download (MB), Upload (MB), Peak Download (Mbps), Peak Upload (Mbps)
3. **Given** some days in the 31-day period have no data, **When** included in the table, **Then** those days show zero values

---

### User Story 3 - Monthly Report with Weekly Graph (Priority: P3)

A user subscribing to monthly email reports receives an enhanced report containing a visual graph showing network usage week-by-week for the last 5 weeks, followed by a summary table displaying the last 31 days of data with the same structure as other reports.

**Why this priority**: Monthly reports provide long-term trend analysis for users who want high-level overviews of their network consumption patterns.

**Independent Test**: Can be fully tested by configuring a monthly report schedule, triggering the report, and verifying the email contains a 5-week graph and 31-day table with correct data.

**Acceptance Scenarios**:

1. **Given** a user has monthly reports enabled, **When** the monthly report is sent, **Then** the email contains a weekly graph (5 data points) showing download and upload totals per week
2. **Given** a user receives a monthly report, **When** viewing the email, **Then** a table displays the last 31 days with columns: Date, Download (MB), Upload (MB), Peak Download (Mbps), Peak Upload (Mbps)
3. **Given** a week in the 5-week period has partial data, **When** displayed in the graph, **Then** the week shows the sum of available data

---

### Edge Cases

- What happens when the report is generated with less than 7 days of data available? The table shows only available days, with a note indicating limited data.
- What happens when the report is generated with less than 31 days of data available? The table shows only available days.
- How does the system handle days/hours with no network activity? Zero values are displayed rather than omitting the time period.
- What happens if the graph cannot be rendered (e.g., image generation fails)? A text-based fallback summary is provided.
- How are weeks defined in the monthly graph? Weeks are calendar weeks (Monday to Sunday).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate frequency-specific email reports with both graph and table components
- **FR-002**: Daily reports MUST include an hourly graph (24 data points: 00:00-23:00) showing download and upload totals (MB per hour)
- **FR-003**: Weekly reports MUST include a daily graph (7 data points for the last 7 days) showing download and upload totals (MB per day)
- **FR-004**: Monthly reports MUST include a weekly graph (5 data points for the last 5 weeks) showing download and upload totals (MB per week)
- **FR-005**: All report types MUST include a summary table with columns: Date, Download (MB), Upload (MB), Peak Download (Mbps), Peak Upload (Mbps)
- **FR-006**: Daily report tables MUST display the last 7 days of data
- **FR-007**: Weekly and Monthly report tables MUST display the last 31 days of data
- **FR-008**: Graphs MUST be embedded in the email as inline images for HTML emails
- **FR-009**: Plain text email versions MUST include a text-based summary replacing the graph
- **FR-010**: Data values MUST be formatted appropriately (MB for totals, Mbps for peak speeds)
- **FR-011**: Graphs MUST clearly distinguish between download and upload data (using distinct colors)
- **FR-012**: Tables MUST include a total/summary row at the bottom

### Key Entities

- **HourlyUsageData**: Represents aggregated network usage for a single hour (download total, upload total, peak speeds)
- **DailyUsageData**: Represents aggregated network usage for a single day (download total, upload total, peak speeds)
- **WeeklyUsageData**: Represents aggregated network usage for a calendar week (download total, upload total, peak speeds)
- **ReportGraph**: Visual representation of usage data over time, with separate series for download and upload
- **ReportTable**: Tabular representation of daily usage data with formatted values

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can visually identify usage patterns at a glance from the graph without reading raw numbers
- **SC-002**: All data in the report table is accurate and matches the underlying usage database within 1% tolerance
- **SC-003**: Email reports render correctly in major email clients (Outlook, Gmail, Apple Mail)
- **SC-004**: Graph images load within 3 seconds on standard email clients
- **SC-005**: Users can compare download vs upload trends using distinct visual elements in the graph
- **SC-006**: Report generation completes within 10 seconds for any frequency type

## Clarifications

### Session 2025-12-21

- Q: Should the hourly graph display speeds (Mbps) or totals (MB)? → A: Totals (MB) - consistent with other frequency graphs and table structure

## Assumptions

- The existing usage database contains hourly granularity data that can be aggregated for all frequency types
- The email infrastructure supports inline images (Content-ID embedding)
- Calendar weeks are defined as Monday through Sunday (ISO week standard)
- Peak speeds are the maximum instantaneous speeds recorded during the period, not averages
- The existing color scheme (blue for download, red for upload) from the application UI will be used in graphs
