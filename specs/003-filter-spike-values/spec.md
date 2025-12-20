# Feature Specification: Filter Impossible Spike Values

**Feature Branch**: `003-filter-spike-values`
**Created**: 2025-12-20
**Status**: Draft
**Input**: User description: "there are super high values from time to time that are impossible. find out where they are from and fix or filter them when they are isolated"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Accurate Real-Time Speed Display (Priority: P1)

As a user monitoring my network usage, I want the speed display to show realistic values so that I can trust the readings and make informed decisions about my network activity.

**Why this priority**: The real-time speed display is the primary interface users interact with. Spike values here are immediately visible and erode user trust in the application.

**Independent Test**: Run the application for 10+ minutes with normal network activity. Verify that displayed speeds never exceed physically possible values for the user's network connection (e.g., no 500 Gbps readings on a 1 Gbps connection).

**Acceptance Scenarios**:

1. **Given** the application is monitoring network usage, **When** the Windows API returns an anomalously high delta value (e.g., counter jump during adapter reconnection), **Then** the speed display shows a filtered/smoothed value within realistic bounds.

2. **Given** normal network activity, **When** speeds are calculated each second, **Then** all displayed values remain within the configured maximum threshold.

3. **Given** a network adapter disconnects and reconnects, **When** the counters reset or jump, **Then** the spike is filtered out and not displayed to the user.

---

### User Story 2 - Accurate Historical Data in Graphs (Priority: P2)

As a user reviewing my usage history, I want the graphs to show accurate data without impossible spikes so that I can understand my actual usage patterns over time.

**Why this priority**: Historical graphs are used for analysis and reporting. Spikes distort averages and make the data unreliable for decision-making.

**Independent Test**: View the usage graph at various time scales (hourly, daily, monthly). Verify no data points show impossibly high values that would distort the graph scale.

**Acceptance Scenarios**:

1. **Given** spike values were recorded in the database, **When** viewing historical graphs, **Then** the spikes are filtered out or capped at realistic maximums.

2. **Given** hourly aggregation runs, **When** calculating peak speeds, **Then** impossible values are excluded from peak calculations.

3. **Given** a graph with mixed valid and spike data, **When** rendered, **Then** the Y-axis scale reflects realistic data ranges, not distorted by outliers.

---

### User Story 3 - Accurate Email Reports (Priority: P3)

As a user receiving scheduled email reports, I want the reported usage and peak speeds to reflect actual usage so that the reports are meaningful and actionable.

**Why this priority**: Email reports are shared externally and represent a summary of usage. Spike values here would make reports look unreliable.

**Independent Test**: Generate a usage report for a period known to contain spike values. Verify the report shows realistic totals and peak speeds.

**Acceptance Scenarios**:

1. **Given** a report is generated for a period with spike data, **When** the email is sent, **Then** total usage and peak speeds reflect filtered/accurate values.

---

### Edge Cases

- What happens when a legitimate high-speed burst occurs (e.g., 10 Gbps fiber connection)? The filter should accommodate the user's actual connection speed.
- How does the system handle multiple rapid adapter reconnections in quick succession? Display last valid reading until stable readings resume.
- What happens when all readings in a time period are spikes (no valid data)? Continue displaying last valid reading; if no valid reading exists, show zero.
- How are existing spike values in the database handled after the fix is deployed? Filter at query/display time; no database migration required.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST detect and filter impossibly high speed readings before displaying them to the user.
- **FR-002**: System MUST apply spike filtering at the data collection layer (before storage) to prevent corrupted data from entering the database.
- **FR-003**: System MUST use a configurable maximum speed threshold that accommodates different network connection speeds (default: 10 Gbps).
- **FR-004**: System MUST handle adapter reconnection scenarios by detecting counter resets/jumps and filtering resulting spikes.
- **FR-005**: System MUST apply spike filtering to peak speed calculations during hourly aggregation.
- **FR-006**: System MUST maintain the existing 1GB per-reading filter for total bytes while adding speed-based filtering.
- **FR-007**: System SHOULD use statistical outlier detection for isolated spikes (e.g., a single spike surrounded by normal readings).
- **FR-008**: System MUST NOT filter legitimate high-speed readings that are consistent with surrounding data points.
- **FR-009**: System MUST display the previous valid reading when a spike is filtered out (hold last good value behavior).

### Key Entities

- **SpeedReading**: Current speed measurement with download/upload bytes per second and timestamp.
- **UsageRecord**: Stored record with timestamp, bytes received, and bytes sent.
- **UsageSummary**: Hourly aggregation with totals, peak speeds, and sample count.
- **SpikeFilter**: Logic component that validates readings against thresholds and historical patterns.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero spike values exceeding 10 Gbps appear in the speed display during normal operation over a 24-hour test period.
- **SC-002**: Historical graphs show no data points exceeding the configured maximum speed threshold.
- **SC-003**: Peak speed values in hourly summaries remain within realistic bounds (no values exceeding maximum threshold).
- **SC-004**: Email reports contain no impossibly high usage totals or peak speeds.
- **SC-005**: Legitimate high-speed network activity (up to the configured threshold) is accurately captured and displayed.

## Clarifications

### Session 2025-12-20

- Q: When a spike value is detected and filtered out, what should be displayed instead? → A: Use previous valid reading (hold last good value)

## Assumptions

- The Windows IP Helper API (GetIfTable2) occasionally returns inconsistent counter values during adapter state changes.
- Most spike values are isolated events (single readings) rather than sustained anomalies.
- A default maximum speed of 10 Gbps is sufficient for most consumer and prosumer network connections.
- Users with faster connections (e.g., 25 Gbps, 100 Gbps) can adjust the threshold in settings.
- Filtering should be aggressive for obviously impossible values (e.g., >100 Gbps) while being conservative for values near the threshold.
