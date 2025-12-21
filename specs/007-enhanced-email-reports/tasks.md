# Tasks: Enhanced Email Reports with Graphs and Tables

**Input**: Design documents from `/specs/007-enhanced-email-reports/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested - skipping test tasks per spec.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/DataUsageReporter/`, `tests/DataUsageReporter.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Add `Week` value to TimeGranularity enum in src/DataUsageReporter/Core/AggregatorTypes.cs
- [ ] T002 [P] Add ReportTableRow record in src/DataUsageReporter/Email/ReportTableRow.cs
- [ ] T003 [P] Add InlineAttachment record in src/DataUsageReporter/Email/InlineAttachment.cs
- [ ] T004 Update EmailMessage record to include InlineAttachments property in src/DataUsageReporter/Email/IReportGenerator.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Add localization keys for table headers to src/DataUsageReporter/Resources/Strings.resx (Email_TableHeader_Date, Email_TableHeader_DownloadMB, Email_TableHeader_UploadMB, Email_TableHeader_PeakDownload, Email_TableHeader_PeakUpload, Email_TableTotal)
- [ ] T006 [P] Add French translations for table headers to src/DataUsageReporter/Resources/Strings.fr.resx
- [ ] T007 Create IEmailReportGraphRenderer interface in src/DataUsageReporter/Email/IEmailReportGraphRenderer.cs
- [ ] T008 Create EmailReportGraphRenderer class with constructor and base setup in src/DataUsageReporter/Email/EmailReportGraphRenderer.cs
- [ ] T009 Update EmailSender.SendAsync to handle InlineAttachments with MailKit LinkedResources in src/DataUsageReporter/Email/EmailSender.cs
- [ ] T010 Add helper method BuildTableRows in ReportGenerator to create ReportTableRow list from summaries in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T011 Add helper method GenerateTableHtml in ReportGenerator to render HTML table from ReportTableRow list in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T012 Add helper method GenerateTablePlainText in ReportGenerator to render ASCII table for plain text emails in src/DataUsageReporter/Email/ReportGenerator.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Daily Report with Hourly Graph (Priority: P1) 🎯 MVP

**Goal**: Daily reports include hourly graph (24 data points) + 7-day table with download/upload totals and peak speeds

**Independent Test**: Configure daily report, trigger send, verify email contains hourly graph image and 7-day table with correct columns

### Implementation for User Story 1

- [ ] T013 [US1] Add localization keys Email_GraphHourly to src/DataUsageReporter/Resources/Strings.resx and Strings.fr.resx
- [ ] T014 [US1] Implement RenderHourlyGraph method in EmailReportGraphRenderer using ScottPlot bar chart (24 bars for hours, download/upload side-by-side) in src/DataUsageReporter/Email/EmailReportGraphRenderer.cs
- [ ] T015 [US1] Add GetHourlyDataForDay method in UsageAggregator to return 24 UsageDataPoint records for a specific day in src/DataUsageReporter/Core/UsageAggregator.cs
- [ ] T016 [US1] Update GenerateReportAsync in ReportGenerator to detect Daily frequency and call RenderHourlyGraph in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T017 [US1] Update GenerateHtmlReport in ReportGenerator to include graph image via CID and 7-day table for Daily frequency in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T018 [US1] Update GeneratePlainTextReport in ReportGenerator to include ASCII hourly summary and 7-day table for Daily frequency in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T019 [US1] Inject IEmailReportGraphRenderer into ReportGenerator constructor in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T020 [US1] Register EmailReportGraphRenderer in dependency injection in src/DataUsageReporter/Program.cs

**Checkpoint**: At this point, User Story 1 (Daily Reports) should be fully functional and testable independently

---

## Phase 4: User Story 2 - Weekly Report with Daily Graph (Priority: P2)

**Goal**: Weekly reports include daily graph (7 data points) + 31-day table with download/upload totals and peak speeds

**Independent Test**: Configure weekly report, trigger send, verify email contains daily graph image and 31-day table with correct columns

### Implementation for User Story 2

- [ ] T021 [US2] Add localization key Email_GraphDaily to src/DataUsageReporter/Resources/Strings.resx and Strings.fr.resx
- [ ] T022 [US2] Implement RenderDailyGraph method in EmailReportGraphRenderer using ScottPlot bar chart (7 bars for days) in src/DataUsageReporter/Email/EmailReportGraphRenderer.cs
- [ ] T023 [US2] Update GenerateReportAsync in ReportGenerator to detect Weekly frequency and call RenderDailyGraph in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T024 [US2] Update GenerateHtmlReport in ReportGenerator to include graph image via CID and 31-day table for Weekly frequency in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T025 [US2] Update GeneratePlainTextReport in ReportGenerator to include ASCII daily summary and 31-day table for Weekly frequency in src/DataUsageReporter/Email/ReportGenerator.cs

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Monthly Report with Weekly Graph (Priority: P3)

**Goal**: Monthly reports include weekly graph (5 data points for 5 calendar weeks) + 31-day table with download/upload totals and peak speeds

**Independent Test**: Configure monthly report, trigger send, verify email contains weekly graph image and 31-day table with correct columns

### Implementation for User Story 3

- [ ] T026 [US3] Add localization keys Email_GraphWeekly and Email_Week to src/DataUsageReporter/Resources/Strings.resx and Strings.fr.resx
- [ ] T027 [US3] Implement GetWeeklyDataPointsAsync method in UsageAggregator using ISOWeek.GetWeekOfYear for calendar week grouping in src/DataUsageReporter/Core/UsageAggregator.cs
- [ ] T028 [US3] Add GetWeeklyDataPointsAsync to IUsageAggregator interface in src/DataUsageReporter/Core/IUsageAggregator.cs
- [ ] T029 [US3] Implement RenderWeeklyGraph method in EmailReportGraphRenderer using ScottPlot bar chart (5 bars for weeks) in src/DataUsageReporter/Email/EmailReportGraphRenderer.cs
- [ ] T030 [US3] Update GenerateReportAsync in ReportGenerator to detect Monthly frequency and call RenderWeeklyGraph in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T031 [US3] Update GenerateHtmlReport in ReportGenerator to include graph image via CID and 31-day table for Monthly frequency in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T032 [US3] Update GeneratePlainTextReport in ReportGenerator to include ASCII weekly summary and 31-day table for Monthly frequency in src/DataUsageReporter/Email/ReportGenerator.cs

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T033 Add edge case handling for empty data periods (show zero values instead of omitting) in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T034 Add fallback text summary if graph rendering fails in src/DataUsageReporter/Email/ReportGenerator.cs
- [ ] T035 Verify graph colors match UI (#4285f4 download blue, #ea4335 upload red) in src/DataUsageReporter/Email/EmailReportGraphRenderer.cs
- [ ] T036 Run manual test: send daily, weekly, monthly reports and verify in Outlook/Gmail
- [ ] T037 Run quickstart.md validation checklist

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
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Independent of US1
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Requires Week granularity (T027-T028 in US3)

### Within Each User Story

- Localization before implementation
- Graph renderer methods before ReportGenerator integration
- Core implementation before edge cases
- Story complete before moving to next priority

### Parallel Opportunities

- T002 and T003 can run in parallel (different files)
- T005 and T006 can run in parallel (different resource files)
- All user stories can start in parallel after Foundational phase (if team capacity allows)

---

## Parallel Example: User Story 1

```bash
# After Foundational phase, launch US1 implementation:
Task: "Add localization keys Email_GraphHourly"
Task: "Implement RenderHourlyGraph method"
# Then sequentially:
Task: "Update GenerateReportAsync for Daily frequency"
Task: "Update GenerateHtmlReport for Daily frequency"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T012)
3. Complete Phase 3: User Story 1 (T013-T020)
4. **STOP and VALIDATE**: Test Daily Report independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Daily)
   - Developer B: User Story 2 (Weekly)
   - Developer C: User Story 3 (Monthly)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Graph size: 600x300 pixels for email compatibility
- Colors: #4285f4 (download blue), #ea4335 (upload red)
