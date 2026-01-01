# Research: GitHub Release Workflow

**Feature**: 009-github-release-workflow
**Date**: 2026-01-01

## Research Tasks

### 1. GitHub Actions for .NET Release

**Decision**: Use `softprops/action-gh-release@v2` for release creation

**Rationale**:
- Most popular and maintained GitHub release action
- Supports auto-generated release notes (GitHub's native feature)
- Supports pre-release flag based on tag pattern
- Simple configuration with file glob patterns for assets

**Alternatives Considered**:
- `actions/create-release` - Deprecated, no longer maintained
- `ncipollo/release-action` - Good alternative but `softprops` has better documentation
- Manual API calls - More complex, no benefit over existing actions

### 2. Version Extraction from Git Tag

**Decision**: Use GitHub Actions native `github.ref_name` context

**Rationale**:
- No external dependencies required
- Tag name available directly as `${{ github.ref_name }}`
- Simple string manipulation to strip 'v' prefix: `${GITHUB_REF_NAME#v}`

**Alternatives Considered**:
- `gitversion` action - Overkill for simple semver tags
- Custom scripts - Unnecessary complexity

### 3. Pre-release Detection

**Decision**: Check if tag contains `-alpha`, `-beta`, or `-rc` using shell pattern matching

**Rationale**:
- Standard semver pre-release conventions
- Simple conditional: `[[ "$TAG" == *"-"* ]]`
- Works with `softprops/action-gh-release` prerelease input

**Alternatives Considered**:
- Regex parsing - More complex, same result
- Separate workflow for pre-releases - Unnecessary duplication

### 4. Self-Contained .NET Publish

**Decision**: Use existing .csproj configuration with `dotnet publish`

**Rationale**:
- Project already configured with `PublishSingleFile=true` and `SelfContained=true`
- Only need to pass `-r win-x64` and version override
- `PublishReadyToRun=true` already set for performance

**Build Command**:
```bash
dotnet publish src/DataUsageReporter/DataUsageReporter.csproj \
  -c Release \
  -r win-x64 \
  -p:Version=$VERSION
```

**Alternatives Considered**:
- Override all publish settings in workflow - Duplicates .csproj, harder to maintain
- Use `dotnet build` + manual packaging - Loses single-file benefits

### 5. GitHub Runner Selection

**Decision**: Use `windows-latest` runner

**Rationale**:
- Required for Windows Forms application
- Includes .NET SDK pre-installed
- Free tier sufficient for release builds

**Alternatives Considered**:
- Self-hosted runner - Unnecessary complexity for infrequent releases
- Ubuntu runner with cross-compile - Not reliable for WinForms

## No Outstanding Research Items

All technical decisions resolved. Ready for implementation.
