# Tasks: Network Usage Monitor

**Input**: Design documents from `/specs/001-network-usage-monitor/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in spec. Test infrastructure included in setup but test tasks omitted per template guidelines.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root (per plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create solution and project structure: `DataUsageReporter.sln`, `src/DataUsageReporter/`, `tests/DataUsageReporter.Tests/`
- [ ] T002 Initialize .NET 8 WinForms project with dependencies (Vanara.PInvoke.IpHlpApi, Microsoft.Data.Sqlite, ScottPlot.WinForms, MailKit) in `src/DataUsageReporter/DataUsageReporter.csproj`
- [ ] T003 [P] Configure single-file publishing settings in `src/DataUsageReporter/DataUsageReporter.csproj` (PublishSingleFile, PublishTrimmed, SelfContained)
- [ ] T004 [P] Create directory structure: `src/DataUsageReporter/Core/`, `Data/`, `Email/`, `UI/`, `Resources/`
- [ ] T005 [P] Initialize xUnit test project with Moq and FluentAssertions in `tests/DataUsageReporter.Tests/DataUsageReporter.Tests.csproj`
- [ ] T006 [P] Add application icon resource in `src/DataUsageReporter/Resources/app.ico`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 Create ISettingsRepository interface and AppSettings model in `src/DataUsageReporter/Data/ISettingsRepository.cs`
- [ ] T008 Implement SettingsRepository with JSON persistence to %AppData% in `src/DataUsageReporter/Data/SettingsRepository.cs`
- [ ] T009 [P] Create ISpeedFormatter interface in `src/DataUsageReporter/Core/ISpeedFormatter.cs`
- [ ] T010 [P] Implement SpeedFormatter (B/s, KB/s, MB/s, GB/s formatting) in `src/DataUsageReporter/Core/SpeedFormatter.cs`
- [ ] T011 Create SQLite database initialization with WAL mode and schema creation in `src/DataUsageReporter/Data/DatabaseInitializer.cs`
- [ ] T012 Create UsageRecord and UsageSummary model classes in `src/DataUsageReporter/Data/Models/UsageRecord.cs` and `UsageSummary.cs`
- [ ] T013 Implement Program.cs with single-instance check and application startup in `src/DataUsageReporter/Program.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Real-Time Taskbar Monitor (Priority: P1) 🎯 MVP

**Goal**: Display real-time download/upload speeds in Windows system tray, updating every second

**Independent Test**: Launch application, observe taskbar displays update every second with accurate network speed readings

### Implementation for User Story 1

- [ ] T014 [P] [US1] Create INetworkMonitor interface with GetCurrentStats() and GetCurrentSpeed() in `src/DataUsageReporter/Core/INetworkMonitor.cs`
- [ ] T015 [P] [US1] Create NetworkStats and SpeedReading record types in `src/DataUsageReporter/Core/NetworkTypes.cs`
- [ ] T016 [US1] Implement NetworkMonitor using IP Helper API (GetIfTable2) in `src/DataUsageReporter/Core/NetworkMonitor.cs`
- [ ] T017 [P] [US1] Create IUsageRepository interface in `src/DataUsageReporter/Data/IUsageRepository.cs`
- [ ] T018 [US1] Implement UsageRepository with SQLite for SaveRecordAsync and GetRecordsSinceAsync in `src/DataUsageReporter/Data/UsageRepository.cs`
- [ ] T019 [P] [US1] Create ITrayIcon interface with UpdateSpeed() and events in `src/DataUsageReporter/UI/ITrayIcon.cs`
- [ ] T020 [US1] Implement TrayIcon with NotifyIcon, dual-line speed display, and context menu in `src/DataUsageReporter/UI/TrayIcon.cs`
- [ ] T021 [US1] Create SpeedDisplay helper for rendering stacked ↓/↑ format in `src/DataUsageReporter/UI/SpeedDisplay.cs`
- [ ] T022 [US1] Implement 1-second Timer loop in Program.cs connecting NetworkMonitor → TrayIcon → UsageRepository
- [ ] T023 [US1] Add Windows startup registry entry (configurable) in `src/DataUsageReporter/Data/StartupManager.cs`
- [ ] T024 [US1] Handle network adapter disconnect/reconnect gracefully in NetworkMonitor
- [ ] T025 [US1] Handle system sleep/wake events to resume monitoring in Program.cs

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Historical Usage Graphs (Priority: P2)

**Goal**: View network usage history through graphs with year/month/day/hour/minute granularity

**Independent Test**: Run application for a period, open options panel, verify graphs display accurate historical data at all granularities

### Implementation for User Story 2

- [ ] T026 [P] [US2] Create IUsageAggregator interface with AggregateHourAsync and GetDataPointsAsync in `src/DataUsageReporter/Core/IUsageAggregator.cs`
- [ ] T027 [P] [US2] Create TimeGranularity enum and DataPoint record in `src/DataUsageReporter/Core/AggregatorTypes.cs`
- [ ] T028 [US2] Implement UsageAggregator with hourly aggregation logic in `src/DataUsageReporter/Core/UsageAggregator.cs`
- [ ] T029 [US2] Add GetSummariesAsync and SaveSummaryAsync to UsageRepository in `src/DataUsageReporter/Data/UsageRepository.cs`
- [ ] T030 [US2] Implement hourly aggregation timer that runs on the hour in Program.cs
- [ ] T031 [US2] Add data retention cleanup (delete records older than 1 year) in `src/DataUsageReporter/Data/RetentionManager.cs`
- [ ] T032 [P] [US2] Create OptionsForm shell with tab control in `src/DataUsageReporter/UI/OptionsForm.cs`
- [ ] T033 [US2] Create GraphPanel with ScottPlot for usage visualization in `src/DataUsageReporter/UI/GraphPanel.cs`
- [ ] T034 [US2] Implement time granularity selector (year/month/day/hour/minute) in GraphPanel
- [ ] T035 [US2] Implement graph data loading with GetDataPointsAsync for selected granularity
- [ ] T036 [US2] Add hover/click tooltip showing exact values on graph data points
- [ ] T037 [US2] Wire OptionsRequested event from TrayIcon to show OptionsForm

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Scheduled Email Reports (Priority: P3)

**Goal**: Schedule automatic email reports of network usage (daily/weekly/monthly)

**Independent Test**: Configure email settings, schedule a report, verify report arrives at scheduled time with accurate usage data

### Implementation for User Story 3

- [ ] T038 [P] [US3] Create ICredentialManager interface in `src/DataUsageReporter/Data/ICredentialManager.cs`
- [ ] T039 [P] [US3] Implement CredentialManager using Windows Credential Manager API in `src/DataUsageReporter/Data/CredentialManager.cs`
- [ ] T040 [P] [US3] Create EmailConfig and ReportSchedule models in `src/DataUsageReporter/Data/Models/EmailConfig.cs` and `ReportSchedule.cs`
- [ ] T041 [US3] Add LoadEmailConfig/SaveEmailConfig and LoadSchedule/SaveSchedule to SettingsRepository
- [ ] T042 [P] [US3] Create IEmailSender interface with SendAsync and TestConnectionAsync in `src/DataUsageReporter/Email/IEmailSender.cs`
- [ ] T043 [US3] Implement EmailSender using MailKit with TLS/SSL support in `src/DataUsageReporter/Email/EmailSender.cs`
- [ ] T044 [P] [US3] Create IReportGenerator interface in `src/DataUsageReporter/Email/IReportGenerator.cs`
- [ ] T045 [US3] Implement ReportGenerator with HTML/plain text report formatting in `src/DataUsageReporter/Email/ReportGenerator.cs`
- [ ] T046 [P] [US3] Create IReportScheduler interface with Start/Stop and events in `src/DataUsageReporter/Email/IReportScheduler.cs`
- [ ] T047 [US3] Implement ReportScheduler with timer-based execution in `src/DataUsageReporter/Email/Scheduler.cs`
- [ ] T048 [US3] Add Email Settings tab to OptionsForm with SMTP configuration fields
- [ ] T049 [US3] Add connection test button that calls TestConnectionAsync
- [ ] T050 [US3] Add Schedule tab to OptionsForm with frequency (daily/weekly/monthly) and time selection
- [ ] T051 [US3] Handle email send failures with notification and retry logic
- [ ] T052 [US3] Handle system sleep/wake to recalculate next scheduled run time

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T053 [P] Add error handling and logging throughout application in `src/DataUsageReporter/Core/Logger.cs`
- [ ] T054 [P] Implement incremental vacuum for SQLite storage optimization
- [ ] T055 Run quickstart.md validation - verify all setup steps work
- [ ] T056 Performance validation: verify <50MB RAM, <5% CPU, <2s startup
- [ ] T057 Build single-file executable and verify size (~15-25MB target)
- [ ] T058 Test Windows 10 and Windows 11 compatibility

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Uses UsageRepository from US1 but independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Uses UsageSummary from US2 but independently testable

### Within Each User Story

- Interfaces before implementations
- Models before services
- Services before UI
- Core implementation before integration

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T003, T004, T005, T006)
- All Foundational tasks marked [P] can run in parallel (T009, T010)
- Within US1: T014, T015, T017, T019 can run in parallel
- Within US2: T026, T027, T032 can run in parallel
- Within US3: T038, T039, T040, T042, T044, T046 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch interface definitions in parallel:
Task: "Create INetworkMonitor interface in src/DataUsageReporter/Core/INetworkMonitor.cs"
Task: "Create NetworkStats and SpeedReading types in src/DataUsageReporter/Core/NetworkTypes.cs"
Task: "Create IUsageRepository interface in src/DataUsageReporter/Data/IUsageRepository.cs"
Task: "Create ITrayIcon interface in src/DataUsageReporter/UI/ITrayIcon.cs"

# Then implementations sequentially:
Task: "Implement NetworkMonitor in src/DataUsageReporter/Core/NetworkMonitor.cs"
Task: "Implement UsageRepository in src/DataUsageReporter/Data/UsageRepository.cs"
Task: "Implement TrayIcon in src/DataUsageReporter/UI/TrayIcon.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready - users get real-time monitoring

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (adds graphs)
4. Add User Story 3 → Test independently → Deploy/Demo (adds email)
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (real-time monitoring)
   - Developer B: User Story 2 (graphs) - can mock UsageRepository data
   - Developer C: User Story 3 (email) - can mock UsageSummary data
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All file paths are relative to repository root
