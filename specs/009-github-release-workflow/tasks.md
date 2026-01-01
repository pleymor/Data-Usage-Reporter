# Tasks: GitHub Release Workflow

**Input**: Design documents from `/specs/009-github-release-workflow/`
**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Tests**: Not requested for this feature (CI/CD workflow tested manually by pushing tags)

**Organization**: Tasks grouped by user story. User Stories 1 & 2 are both P1 and tightly coupled (same workflow file), so they are combined. User Story 3 (P2) adds version injection.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Create workflow directory structure

- [x] T001 Create .github/workflows/ directory if not exists

---

## Phase 2: User Story 1 & 2 - Automated Release with Single-File Executable (Priority: P1) 🎯 MVP

**Goal**: Create a GitHub Actions workflow that triggers on version tags, builds a self-contained single-file Windows executable, and publishes it as a GitHub release with auto-generated release notes.

**Independent Test**: Push a tag `v1.2.1` to the repository and verify:
1. Workflow runs on GitHub Actions
2. GitHub release is created with title `v1.2.1`
3. Release includes `DataUsageReporter-1.2.1-win-x64.exe` as downloadable asset
4. Release notes are auto-generated from commits

### Implementation

- [x] T002 [US1] Create workflow file skeleton with tag trigger in .github/workflows/release.yml
- [x] T003 [US1] Add checkout step using actions/checkout@v4 in .github/workflows/release.yml
- [x] T004 [US1] Add .NET 8.0 SDK setup step using actions/setup-dotnet@v4 in .github/workflows/release.yml
- [x] T005 [US1] Add version extraction step (strip 'v' prefix from tag) in .github/workflows/release.yml
- [x] T006 [US2] Add dotnet publish step with self-contained single-file configuration in .github/workflows/release.yml
- [x] T007 [US1] Add artifact rename step to apply naming convention in .github/workflows/release.yml
- [x] T008 [US1] Add pre-release detection step for alpha/beta/rc tags in .github/workflows/release.yml
- [x] T009 [US1] Add GitHub release creation using softprops/action-gh-release@v2 in .github/workflows/release.yml

**Checkpoint**: Workflow should now build and release on version tag push

---

## Phase 3: User Story 3 - Version Consistency (Priority: P2)

**Goal**: Ensure the built executable's version matches the git tag by passing the version to dotnet publish.

**Independent Test**: After release, download executable, run it, check About dialog shows the tag version.

### Implementation

- [x] T010 [US3] Update dotnet publish step to pass version from tag as -p:Version parameter in .github/workflows/release.yml

**Checkpoint**: Version in About dialog now matches tag version

---

## Phase 4: Polish & Validation

**Purpose**: Final validation and documentation

- [x] T011 Validate workflow syntax using GitHub Actions linter or dry-run
- [x] T012 Update quickstart.md with actual workflow file location in specs/009-github-release-workflow/quickstart.md
- [ ] T013 Test workflow by creating and pushing a test tag (e.g., v1.2.1-test) [MANUAL - requires push to GitHub]

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - create directory
- **User Story 1 & 2 (Phase 2)**: Depends on Setup - core workflow implementation
- **User Story 3 (Phase 3)**: Depends on Phase 2 - version injection enhancement
- **Polish (Phase 4)**: Depends on all phases - final validation

### Task Dependencies Within Phases

**Phase 2 (Sequential - same file)**:
```
T002 → T003 → T004 → T005 → T006 → T007 → T008 → T009
```
All tasks modify the same file sequentially to build up the workflow.

**Phase 3**:
```
T010 (modifies existing publish step)
```

### Parallel Opportunities

Limited parallelism as this feature is a single YAML file. However:
- T011 and T012 in Phase 4 can run in parallel [P]

---

## Implementation Strategy

### MVP First (User Stories 1 & 2)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Core workflow (T002-T009)
3. **STOP and VALIDATE**: Push a test tag and verify release is created
4. If working: MVP complete!

### Full Implementation

1. Complete MVP
2. Add User Story 3 (T010) - version injection
3. Validate version appears correctly
4. Complete Polish phase (T011-T013)

---

## Notes

- All Phase 2 tasks modify the same file (.github/workflows/release.yml) - execute sequentially
- Test workflow by pushing a pre-release tag first (e.g., v1.2.1-test) to avoid polluting releases
- The workflow only triggers on tags matching `v*.*.*` pattern
- Pre-release tags (-alpha, -beta, -rc) will be marked as pre-releases on GitHub
