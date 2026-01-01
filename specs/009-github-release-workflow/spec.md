# Feature Specification: GitHub Release Workflow

**Feature Branch**: `009-github-release-workflow`
**Created**: 2026-01-01
**Status**: Draft
**Input**: User description: "create a github workflow to release builds"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automated Release on Version Tag (Priority: P1)

As a maintainer, I want the application to be automatically built and published as a GitHub release when I push a version tag, so that users can download the latest version without manual intervention.

**Why this priority**: This is the core functionality - without automated releases, the workflow has no value.

**Independent Test**: Can be fully tested by pushing a version tag (e.g., `v1.2.0`) to the repository and verifying a GitHub release is created with the built executable.

**Acceptance Scenarios**:

1. **Given** a maintainer pushes a tag matching `v*.*.*` pattern, **When** the workflow runs, **Then** a GitHub release is created with the tag name as the release title
2. **Given** a maintainer pushes a tag `v1.2.0`, **When** the build completes successfully, **Then** the release includes a downloadable Windows executable
3. **Given** a maintainer pushes a tag, **When** the workflow runs, **Then** release notes are automatically generated from commit history since last tag

---

### User Story 2 - Self-Contained Single-File Executable (Priority: P1)

As a user, I want to download a single executable file that runs without requiring .NET runtime installation, so that I can use the application immediately without additional setup.

**Why this priority**: Users expect a simple download-and-run experience; requiring runtime installation creates friction.

**Independent Test**: Can be tested by downloading the released executable on a clean Windows machine without .NET installed and verifying it runs.

**Acceptance Scenarios**:

1. **Given** a release is created, **When** a user downloads the executable, **Then** the download is a single `.exe` file
2. **Given** a user has Windows 10/11 without .NET 8 installed, **When** they run the downloaded executable, **Then** the application starts successfully

---

### User Story 3 - Version Consistency (Priority: P2)

As a maintainer, I want the released executable's version to match the git tag, so that users can verify they have the correct version.

**Why this priority**: Important for troubleshooting and support but not critical for basic functionality.

**Independent Test**: Can be tested by checking the application's About dialog shows the same version as the release tag.

**Acceptance Scenarios**:

1. **Given** a release is created from tag `v1.2.0`, **When** a user runs the executable and checks the About dialog, **Then** the version displayed is `1.2.0`

---

### Edge Cases

- What happens when a tag is pushed but the build fails? The workflow should not create a release and should report the failure.
- What happens when a tag doesn't follow the `v*.*.*` pattern? The workflow should not trigger.
- What happens when a tag is pushed for a pre-release version (e.g., `v1.2.0-beta`)? The release should be marked as pre-release.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Workflow MUST trigger only on tags matching semantic versioning pattern (`v*.*.*`)
- **FR-002**: Workflow MUST build the application as a self-contained, single-file Windows executable
- **FR-003**: Workflow MUST create a GitHub release with the tag name as the title
- **FR-004**: Workflow MUST attach the built executable to the release as `DataUsageReporter-{version}-win-x64.exe`
- **FR-005**: Workflow MUST generate release notes from commits since the previous tag
- **FR-006**: Workflow MUST mark releases from tags containing `-alpha`, `-beta`, or `-rc` as pre-releases
- **FR-007**: Workflow MUST NOT create a release if the build fails
- **FR-008**: Workflow MUST extract version number from tag and update the build accordingly
- **FR-009**: Built executable MUST be compatible with Windows 10 and Windows 11 (x64)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Releases are automatically published within 10 minutes of pushing a version tag
- **SC-002**: Users can download and run the application without installing any prerequisites
- **SC-003**: 100% of releases contain a functional single-file executable
- **SC-004**: Version displayed in application matches the release tag version
- **SC-005**: Manual release process is eliminated (zero manual steps required after tagging)

## Assumptions

- The repository uses GitHub as its hosting platform
- Tags follow semantic versioning format (`v1.2.0`, `v1.2.0-beta.1`, etc.)
- Windows x64 is the only target platform (no Linux/macOS builds required)
- The existing build configuration in the `.csproj` file supports single-file publishing

## Clarifications

### Session 2026-01-01

- Q: What naming convention should be used for release assets? → A: `DataUsageReporter-{version}-win-x64.exe` (includes platform)
