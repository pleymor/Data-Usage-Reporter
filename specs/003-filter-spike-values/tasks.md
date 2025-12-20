# Tasks: Filter Impossible Spike Values

**Input**: Design documents from `/specs/003-filter-spike-values/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not requested in specification - manual verification only.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No setup required - extending existing project structure.

*No tasks - project already initialized with all dependencies.*

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add threshold configuration that all user stories depend on.

- [x] T001 Add MaxSpeedThresholdGbps property (default: 10) to src/DataUsageReporter/Data/Settings.cs
- [x] T002 Add BytesPerGbps constant (125_000_000L) to src/DataUsageReporter/Core/NetworkMonitor.cs

**Checkpoint**: Threshold configuration ready - user story implementation can begin.

---

## Phase 3: User Story 1 - Accurate Real-Time Speed Display (Priority: P1) MVP

**Goal**: Filter spike values in real-time speed display using threshold check and hold-last-value behavior.

**Independent Test**: Run application for 10+ minutes. Verify speed display never shows values exceeding 10 Gbps (or configured threshold).

### Implementation for User Story 1

- [x] T003 [US1] Add _lastValidSpeed field (SpeedReading) to src/DataUsageReporter/Core/NetworkMonitor.cs
- [x] T004 [US1] Add _maxBytesPerSecond field initialized from settings in NetworkMonitor constructor in src/DataUsageReporter/Core/NetworkMonitor.cs
- [x] T005 [US1] Add IsSpike(long downloadBps, long uploadBps) private method to src/DataUsageReporter/Core/NetworkMonitor.cs
- [x] T006 [US1] Modify GetCurrentSpeed() to check IsSpike and return _lastValidSpeed when spike detected in src/DataUsageReporter/Core/NetworkMonitor.cs
- [x] T007 [US1] Update _lastValidSpeed when valid reading received in src/DataUsageReporter/Core/NetworkMonitor.cs
- [x] T008 [US1] Pass Settings to NetworkMonitor constructor if not already available in src/DataUsageReporter/Program.cs

**Checkpoint**: Real-time speed display filters spikes. US1 complete and testable.

---

## Phase 4: User Story 2 - Accurate Historical Data in Graphs (Priority: P2)

**Goal**: Filter spike values in hourly aggregation peak speed calculations.

**Independent Test**: View usage graphs at hourly/daily/monthly scales. Verify no data points exceed configured threshold.

### Implementation for User Story 2

- [x] T009 [US2] Add _maxBytesPerSecond field to src/DataUsageReporter/Core/UsageAggregator.cs
- [x] T010 [US2] Pass Settings to UsageAggregator constructor in src/DataUsageReporter/Program.cs
- [x] T011 [US2] Modify peak speed calculation in AggregateHourAsync to skip values exceeding threshold in src/DataUsageReporter/Core/UsageAggregator.cs

**Checkpoint**: Historical graphs show filtered data. US2 complete and testable.

---

## Phase 5: User Story 3 - Accurate Email Reports (Priority: P3)

**Goal**: Ensure email reports use filtered data from aggregated summaries.

**Independent Test**: Generate email report for period with known spike data. Verify totals and peak speeds are realistic.

### Implementation for User Story 3

*No implementation tasks required - email reports already use UsageSummary data which is now filtered by US2.*

**Checkpoint**: Email reports automatically benefit from US2 filtering. US3 complete and testable.

---

## Phase 6: Polish & Verification

**Purpose**: Final verification and cleanup.

- [x] T012 Build and run application to verify spike filtering works
- [x] T013 Verify real-time speed display shows realistic values (US1)
- [x] T014 Verify historical graphs show no spike distortion (US2)
- [x] T015 Verify email report shows realistic peak speeds (US3) - Centralized filtering in UsageAggregator.GetFilteredPeakSpeedsAsync()

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A - no setup required
- **Foundational (Phase 2)**: No dependencies - can start immediately
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2)
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) - can run parallel with US1
- **User Story 3 (Phase 5)**: Depends on User Story 2 (uses aggregated data)
- **Polish (Phase 6)**: Depends on all user stories

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational only - INDEPENDENT
- **User Story 2 (P2)**: Depends on Foundational only - INDEPENDENT (can run parallel with US1)
- **User Story 3 (P3)**: Depends on User Story 2 (inherits filtering from aggregation)

### Within Each User Story

- T003-T007 are sequential (modify same file, same method)
- T009-T011 are sequential (modify same file)

### Parallel Opportunities

- T001 and T002 modify different files - can run in parallel
- US1 (T003-T008) and US2 (T009-T011) can run in parallel after Foundational
- T012-T015 are verification tasks that depend on implementation

---

## Parallel Example: Foundational Phase

```text
# These can run in parallel (different files):
T001: Add MaxSpeedThresholdGbps to Settings.cs
T002: Add BytesPerGbps constant to NetworkMonitor.cs
```

## Parallel Example: User Stories

```text
# After Foundational complete, US1 and US2 can run in parallel:
US1 (T003-T008): Modify NetworkMonitor.cs
US2 (T009-T011): Modify UsageAggregator.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (T001-T002)
2. Complete Phase 3: User Story 1 (T003-T008)
3. **STOP and VALIDATE**: Test real-time speed display
4. Build and verify spikes are filtered

### Full Implementation

1. Complete Foundational (T001-T002)
2. Complete US1 (T003-T008) - Real-time display filtering
3. Complete US2 (T009-T011) - Historical data filtering
4. US3 automatically complete (uses filtered aggregation)
5. Complete Polish (T012-T015) - Verification

---

## Notes

- All implementation modifies existing files (no new files created)
- US3 requires no code changes - benefits from US2 automatically
- Manual verification sufficient per constitution (Test Discipline principle)
- Commit after each user story completion
