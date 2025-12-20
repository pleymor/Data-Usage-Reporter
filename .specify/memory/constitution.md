<!--
=============================================================================
Sync Impact Report
=============================================================================
Version change: 0.0.0 → 1.0.0 (initial ratification)

Modified principles: N/A (initial version)

Added sections:
- Core Principles (5 principles: Lightweight First, Windows Native, Resource
  Efficiency, Simplicity, Test Discipline)
- Platform Constraints
- Development Workflow
- Governance

Removed sections: N/A (initial version)

Templates requiring updates:
- .specify/templates/plan-template.md: ✅ No updates needed (Constitution Check
  section already references constitution dynamically)
- .specify/templates/spec-template.md: ✅ No updates needed (generic template)
- .specify/templates/tasks-template.md: ✅ No updates needed (generic template)

Follow-up TODOs: None
=============================================================================
-->

# Data Usage Reporter Constitution

## Core Principles

### I. Lightweight First

All features MUST minimize external dependencies and binary size. Every dependency
added MUST be justified with a clear need that cannot be met by standard library
functionality. Prefer single-file solutions over multi-module architectures where
feasible. The application MUST remain distributable as a single executable or
minimal file set.

**Rationale**: A lightweight application reduces installation complexity, minimizes
attack surface, and ensures fast startup times.

### II. Windows Native

All code MUST run natively on Windows without requiring WSL, Cygwin, or other
compatibility layers. File paths MUST use platform-appropriate separators or
cross-platform path handling. All shell commands and scripts MUST be PowerShell
or batch-compatible. External tools MUST be available via standard Windows package
managers (winget, chocolatey) or bundled with the application.

**Rationale**: The primary target platform is Windows; native support ensures
reliable operation and reduces user friction.

### III. Resource Efficiency

The application MUST operate within strict resource constraints:
- Peak memory usage MUST NOT exceed 100MB during normal operation
- CPU usage MUST remain below 10% during idle/monitoring periods
- Startup time MUST be under 2 seconds on standard hardware

All algorithms MUST prefer memory-efficient approaches over speed when trade-offs
exist. Streaming and lazy evaluation MUST be used for large data processing.

**Rationale**: Low resource consumption enables the application to run alongside
other tools without impacting system performance.

### IV. Simplicity

Solutions MUST use the simplest approach that meets requirements. YAGNI (You Aren't
Gonna Need It) principles apply strictly:
- No abstraction layers unless three or more concrete implementations exist
- No configuration options for hypothetical future needs
- No framework overhead when standard library suffices

Code MUST be readable without extensive documentation. Function and variable names
MUST be self-documenting.

**Rationale**: Simplicity reduces maintenance burden, improves debuggability, and
keeps the codebase accessible.

### V. Test Discipline

All public interfaces MUST have corresponding tests. Tests MUST:
- Run quickly (full suite under 30 seconds)
- Require no external services or network access
- Be deterministic and reproducible

Integration tests MAY be added for critical paths but MUST NOT slow the test suite
significantly.

**Rationale**: Fast, reliable tests enable confident refactoring and catch
regressions early without disrupting development flow.

## Platform Constraints

- **Target OS**: Windows 10/11 (64-bit)
- **Runtime**: No external runtime requirements preferred; if unavoidable,
  must be commonly pre-installed or trivially installable
- **Permissions**: MUST NOT require administrator privileges for normal operation
- **Storage**: MUST use standard Windows locations (AppData, temp directories)
- **Encoding**: MUST handle UTF-8 and Windows-1252 gracefully

## Development Workflow

- All changes MUST be tested on Windows before merge
- PowerShell scripts MUST be compatible with PowerShell 5.1+ (Windows built-in)
- Build artifacts MUST be self-contained where possible
- Version control MUST follow semantic versioning (MAJOR.MINOR.PATCH)

## Governance

This constitution establishes non-negotiable principles for the Data Usage Reporter
project. All code contributions, design decisions, and architectural changes MUST
comply with these principles.

**Amendment Process**:
1. Propose changes via documented discussion
2. Evaluate impact on existing code and principles
3. Update constitution version following semantic versioning:
   - MAJOR: Principle removal or incompatible redefinition
   - MINOR: New principle or significant expansion
   - PATCH: Clarification or minor refinement
4. Update dependent templates if principles change

**Compliance Review**: All pull requests MUST verify alignment with constitution
principles. Violations MUST be documented with justification or resolved before
merge.

**Version**: 1.0.0 | **Ratified**: 2025-12-19 | **Last Amended**: 2025-12-19
