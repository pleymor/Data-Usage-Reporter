# Feature Specification: Unified Graph

**Feature Branch**: `005-unified-graph`
**Created**: 2025-12-20
**Status**: Draft
**Input**: User description: "simplify graphs. keep only 1 graph containing all data"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View All Network Data in One Graph (Priority: P1)

A user opens the Options dialog and navigates to the Usage Graph tab. They see a single unified graph that displays all network data (download and upload) together, making it easy to understand their network usage at a glance without switching between views or graphs.

**Why this priority**: This is the core feature - providing a simplified, consolidated view of network usage data. Users need a single place to see all their data without complexity.

**Independent Test**: Can be fully tested by opening the Options dialog and viewing the graph. Delivers a clear, unified visualization of network usage.

**Acceptance Scenarios**:

1. **Given** the user opens the Options dialog, **When** they view the Usage Graph tab, **Then** they see a single graph displaying both download and upload data together
2. **Given** the graph is displayed, **When** the user selects different time ranges (60 minutes, 24 hours, 30 days, etc.), **Then** the single graph updates to show all data for that period
3. **Given** the graph shows data, **When** the user looks at the visualization, **Then** they can distinguish between download and upload values through legend and colors

---

### User Story 2 - Clear Visual Distinction Between Data Types (Priority: P2)

While viewing the unified graph, the user can clearly distinguish between download and upload data through visual cues such as different colors, without the need for separate graphs.

**Why this priority**: Essential for usability - users must be able to interpret the data correctly. Without clear distinction, the unified graph would be confusing.

**Independent Test**: Can be tested by viewing the graph and confirming download/upload lines are visually distinct and labeled in a legend.

**Acceptance Scenarios**:

1. **Given** the unified graph is displayed, **When** the user looks at the graph, **Then** download data is shown in one distinct color and upload data in another
2. **Given** the graph has both data series, **When** the user checks the legend, **Then** both "Download" and "Upload" are clearly labeled with their respective colors

---

### Edge Cases

- What happens when there is no data available? The graph displays a "No data available" message.
- What happens when only download or only upload data exists? The graph displays the available data series only.
- How does the system handle very sparse data points? Lines connect available points without visual gaps.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a single unified graph on the Usage Graph tab showing both download and upload data
- **FR-002**: System MUST visually distinguish download data from upload data using different colors
- **FR-003**: System MUST display a legend identifying the download and upload data series
- **FR-004**: System MUST support time granularity selection (minute, hour, day, month, year) for the unified graph
- **FR-005**: System MUST display data in Mbps units on the Y-axis
- **FR-006**: System MUST display time on the X-axis with appropriate date/time formatting based on granularity
- **FR-007**: System MUST show a clear message when no data is available

### Key Entities

- **UsageDataPoint**: Represents a single data point with timestamp, download bytes, and upload bytes
- **TimeGranularity**: Defines the aggregation level (minute, hour, day, month, year)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view all network usage data (download and upload) in a single graph without switching views
- **SC-002**: Users can identify download vs upload data within 2 seconds of viewing the graph
- **SC-003**: Time range changes update the graph within 1 second
- **SC-004**: 100% of historical data is accessible through the single graph interface

## Assumptions

- The current GraphPanel implementation already shows download and upload on a single graph - the "simplification" may refer to ensuring no additional graphs are added in the future or confirming the single-graph approach is correct
- The existing color scheme (green for download, orange for upload) is acceptable
- The existing time granularity options (60 minutes, 24 hours, 30 days, 12 months, 5 years) are sufficient
- No additional data types beyond download and upload need to be displayed
