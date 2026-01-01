# Implementation Plan: GitHub Release Workflow

**Branch**: `009-github-release-workflow` | **Date**: 2026-01-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/009-github-release-workflow/spec.md`

## Summary

Create a GitHub Actions workflow that automatically builds and publishes releases when version tags are pushed. The workflow will produce a self-contained single-file Windows executable and attach it to a GitHub release with auto-generated release notes.

## Technical Context

**Language/Version**: YAML (GitHub Actions) + C# .NET 8.0 (existing build)
**Primary Dependencies**: GitHub Actions (`actions/checkout`, `actions/setup-dotnet`, `softprops/action-gh-release`)
**Storage**: N/A (CI/CD workflow)
**Testing**: Workflow tested by pushing a version tag
**Target Platform**: GitHub-hosted runners (windows-latest) building for Windows x64
**Project Type**: single (adds workflow file to existing project)
**Performance Goals**: Release published within 10 minutes of tag push
**Constraints**: Must use existing .csproj publish configuration
**Scale/Scope**: Single workflow file, triggered on version tags

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Lightweight First | PASS | Single YAML file, no new app dependencies |
| II. Windows Native | PASS | Builds native Windows executable |
| III. Resource Efficiency | N/A | CI/CD infrastructure, not runtime |
| IV. Simplicity | PASS | Uses standard GitHub Actions patterns |
| V. Test Discipline | PASS | Build verification before release |

**Platform Constraints:**
- Target OS: Windows 10/11 (64-bit) ✓
- Runtime: Self-contained, no runtime required ✓
- Semantic versioning: Tags use `v*.*.*` format ✓

## Project Structure

### Documentation (this feature)

```text
specs/009-github-release-workflow/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
.github/
└── workflows/
    └── release.yml      # NEW: Release workflow
```

**Structure Decision**: This feature adds a single workflow file to the `.github/workflows/` directory. No changes to application source code are required since the version is already read dynamically from the assembly.

## Complexity Tracking

No constitution violations. Feature is minimal and aligned with all principles.

## Implementation Approach

### Workflow Trigger

```yaml
on:
  push:
    tags:
      - 'v*.*.*'
```

### Key Steps

1. **Checkout** - Get source code at the tagged commit
2. **Setup .NET** - Install .NET 8.0 SDK on runner
3. **Extract Version** - Parse version from tag (strip 'v' prefix)
4. **Build** - Run `dotnet publish` with version override
5. **Rename Artifact** - Apply naming convention `DataUsageReporter-{version}-win-x64.exe`
6. **Create Release** - Use `softprops/action-gh-release` with:
   - Auto-generated release notes
   - Pre-release flag for `-alpha`, `-beta`, `-rc` tags
   - Attached executable

### Build Command

```bash
dotnet publish src/DataUsageReporter/DataUsageReporter.csproj \
  -c Release \
  -r win-x64 \
  -p:Version={version} \
  --self-contained true \
  -p:PublishSingleFile=true
```

## Dependencies

- `actions/checkout@v4` - Standard checkout action
- `actions/setup-dotnet@v4` - .NET SDK setup
- `softprops/action-gh-release@v2` - GitHub release creation with auto-generated notes
