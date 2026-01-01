# Quickstart: GitHub Release Workflow

**Feature**: 009-github-release-workflow

## Overview

This feature adds automated release builds. When you push a version tag, GitHub Actions automatically builds the application and creates a release.

## How to Create a Release

### 1. Update Version (Optional)

The version in `src/DataUsageReporter/DataUsageReporter.csproj` will be overridden by the git tag during CI build. You can optionally update it locally for consistency:

```xml
<Version>1.3.0</Version>
```

### 2. Commit and Push Changes

```bash
git add .
git commit -m "Prepare release 1.3.0"
git push
```

### 3. Create and Push Version Tag

```bash
git tag v1.3.0
git push origin v1.3.0
```

### 4. Wait for Workflow

The GitHub Actions workflow will:
1. Build the application
2. Create a GitHub release
3. Attach `DataUsageReporter-1.3.0-win-x64.exe`
4. Generate release notes from commits

### 5. Verify Release

Check the [Releases page](../../releases) for the new release.

## Pre-release Versions

For alpha, beta, or release candidate versions:

```bash
git tag v1.3.0-beta.1
git push origin v1.3.0-beta.1
```

These are automatically marked as pre-releases on GitHub.

## Troubleshooting

### Build Failed

Check the Actions tab for error details. Common issues:
- Missing dependencies
- Compilation errors
- Test failures

### Release Not Created

Ensure:
- Tag follows `v*.*.*` pattern (e.g., `v1.2.3`)
- Tag was pushed to `origin` (not just local)
- Workflow has repository permissions

## Files

| File | Purpose |
|------|---------|
| `.github/workflows/release.yml` | GitHub Actions workflow |
