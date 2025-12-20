# Tasks: Unified Graph

**Input**: Design documents from `/specs/005-unified-graph/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests not explicitly requested - test tasks excluded.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/DataUsageReporter/`, `tests/DataUsageReporter.Tests/` at repository root
- Paths follow project structure from plan.md

---

## Phase 1: Setup

**Purpose**: Project verification and documentation

- [x] T001 Verify project builds successfully with `dotnet build src/DataUsageReporter/DataUsageReporter.csproj`
- [x] T002 Verify existing tests pass with `dotnet test tests/DataUsageReporter.Tests/DataUsageReporter.Tests.csproj`

---

## Phase 2: Foundational (Verification)

**Purpose**: Confirm existing implementation meets all requirements before any changes

**⚠️ CRITICAL**: This feature primarily documents existing behavior - verify before modifying

- [x] T003 Review existing GraphPanel implementation in src/DataUsageReporter/UI/GraphPanel.cs
- [x] T004 Verify single graph displays both download and upload data
- [x] T005 Verify distinct colors used (green #4CAF50 for download, orange #FF9800 for upload)
- [x] T006 Verify legend displays "Download" and "Upload" labels
- [x] T007 Verify time granularity selector works (minute, hour, day, month, year)
- [x] T008 Verify "No data available" message displays when no data exists

**Checkpoint**: If all verifications pass, minimal code changes needed - proceed to documentation

---

## Phase 3: User Story 1 - View All Network Data in One Graph (Priority: P1) 🎯 MVP

**Goal**: Confirm single unified graph displays both download and upload data together

**Independent Test**: Open Options dialog, navigate to Usage Graph tab, verify single graph shows both data series

### Implementation for User Story 1

- [x] T009 [US1] Add documentation comment in src/DataUsageReporter/UI/GraphPanel.cs confirming single-graph design decision
- [x] T010 [US1] Verify UpdatePlot method in src/DataUsageReporter/UI/GraphPanel.cs renders both download and upload on same plot
- [x] T011 [US1] Verify RefreshDataAsync in src/DataUsageReporter/UI/GraphPanel.cs retrieves both data series
- [x] T012 [US1] Verify granularity dropdown in src/DataUsageReporter/UI/GraphPanel.cs updates single graph (not multiple)

**Checkpoint**: User Story 1 complete - single unified graph confirmed and documented

---

## Phase 4: User Story 2 - Clear Visual Distinction Between Data Types (Priority: P2)

**Goal**: Ensure download and upload data are visually distinguishable through colors and legend

**Independent Test**: View graph and confirm colors are distinct, legend identifies both series

### Implementation for User Story 2

- [x] T013 [US2] Verify download line color (#4CAF50 green) in src/DataUsageReporter/UI/GraphPanel.cs UpdatePlot method
- [x] T014 [US2] Verify upload line color (#FF9800 orange) in src/DataUsageReporter/UI/GraphPanel.cs UpdatePlot method
- [x] T015 [US2] Verify legend text "Download" and "Upload" in src/DataUsageReporter/UI/GraphPanel.cs UpdatePlot method
- [x] T016 [US2] Verify legend position (upper right) in src/DataUsageReporter/UI/GraphPanel.cs UpdatePlot method

**Checkpoint**: User Story 2 complete - visual distinction verified

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and edge case handling

- [x] T017 [P] Update CLAUDE.md with unified graph design notes if needed
- [x] T018 [P] Verify edge case: empty data displays "No data available" message
- [x] T019 [P] Verify edge case: sparse data points connect without gaps
- [x] T020 Run application and manually test all acceptance scenarios from spec.md
- [x] T021 Run quickstart.md validation steps

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS user stories if issues found
- **User Stories (Phase 3-4)**: Depend on Foundational verification passing
- **Polish (Phase 5)**: Depends on User Story phases complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational verification - No dependencies on US2
- **User Story 2 (P2)**: Can start after Foundational verification - No dependencies on US1

### Within Each User Story

- Verification tasks before documentation updates
- All tasks in each story are sequential (same file: GraphPanel.cs)

### Parallel Opportunities

- T001 and T002 (Setup) can run in parallel
- T017, T018, T019 (Polish) can run in parallel
- US1 and US2 could run in parallel but both modify same file

---

## Parallel Example: Setup Phase

```bash
# Launch setup tasks together:
Task: "Verify project builds successfully"
Task: "Verify existing tests pass"
```

## Parallel Example: Polish Phase

```bash
# Launch polish tasks together:
Task: "Update CLAUDE.md with unified graph design notes"
Task: "Verify edge case: empty data displays message"
Task: "Verify edge case: sparse data points connect"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (build verification)
2. Complete Phase 2: Foundational (existing implementation verification)
3. Complete Phase 3: User Story 1 (document single-graph design)
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready - feature is primarily documentation of existing behavior

### Incremental Delivery

1. Complete Setup + Foundational → Verify existing implementation
2. Add User Story 1 → Document design decision → Validate
3. Add User Story 2 → Verify visual distinction → Validate
4. Complete Polish → Full feature validation

### Key Insight

This feature is primarily a **verification and documentation** effort. The existing GraphPanel implementation already satisfies all requirements. The tasks focus on:
1. Confirming existing behavior matches spec
2. Adding documentation to codify the design decision
3. Validating edge cases work correctly

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Most tasks are verification/documentation rather than implementation
- Existing GraphPanel.cs already implements unified graph pattern
- Commit documentation changes after verification passes
- Stop at any checkpoint to validate independently
