# Tasks: Graph Shows All Historical Data (Bug Fix)

**Input**: Design documents from `/specs/008-extended-graph-history/`
**Prerequisites**: plan.md, spec.md, research.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1)
- Include exact file paths in descriptions

---

## Phase 1: User Story 1 - View All Historical Data (Priority: P1) 🎯 MVP

**Goal**: Fix the graph to display all historical usage data instead of only today's data

**Independent Test**: Open the Options dialog, navigate to the Usage Graph tab, and verify the graph displays data from previous days/weeks/months

### Implementation for User Story 1

- [x] T001 [US1] Change TimeGranularity from Minute to Hour in src/DataUsageReporter/UI/GraphPanel.cs:67

**Checkpoint**: Graph should now display all historical data from usage_summaries table

---

## Phase 2: Verification

**Purpose**: Confirm the fix works correctly

- [x] T002 Build the application and verify no compilation errors
- [x] T003 Run the application and open Options → Usage Graph tab
- [x] T004 Verify graph displays data from previous days (if historical data exists in database)
- [x] T005 Verify X-axis shows date range spanning multiple days

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Implementation)**: No dependencies - single line change
- **Phase 2 (Verification)**: Depends on Phase 1 completion

### Execution

1. T001: Apply the fix
2. T002-T005: Verify sequentially

---

## Implementation Strategy

### MVP (Single Task)

1. Complete T001 (the only code change required)
2. Build and verify with T002-T005
3. Done - minimal bug fix complete

---

## Notes

- This is a single-line bug fix
- No new files or dependencies required
- Change `TimeGranularity.Minute` to `TimeGranularity.Hour` in GraphPanel.cs
- The existing `GetSummariesAsync()` and `AggregateByHour()` methods already work correctly
