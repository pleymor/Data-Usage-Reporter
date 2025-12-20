# Research: About Tab

**Feature**: 002-credits-tab
**Date**: 2025-12-20

## Research Tasks

### 1. Third-Party Library Licenses

**Task**: Identify license types for all third-party libraries used.

**Findings**:

| Library | License | Attribution Required |
|---------|---------|---------------------|
| MailKit | MIT License | Yes - copyright notice |
| DnsClient | Apache 2.0 | Yes - copyright notice |
| ScottPlot | MIT License | Yes - copyright notice |
| Microsoft.Data.Sqlite | MIT License | Yes - copyright notice |
| Vanara.PInvoke.IpHlpApi | MIT License | Yes - copyright notice |

**Decision**: All libraries use permissive open-source licenses (MIT/Apache 2.0). Simple attribution with library name and license type is sufficient.

**Rationale**: MIT and Apache 2.0 only require preservation of copyright notice and license text. For an About dialog, showing the license type is standard practice.

### 2. Version Information Retrieval

**Task**: Best approach to get application version in .NET.

**Decision**: Use `System.Reflection.Assembly.GetExecutingAssembly().GetName().Version`

**Rationale**: This is the standard .NET approach, reads from the assembly metadata set in the .csproj file, and requires no additional dependencies.

**Alternatives considered**:
- File version attribute: More complex, same result
- Hardcoded string: Would require manual updates

### 3. Scrollable Content in WinForms

**Task**: Best approach for scrollable content in a tab.

**Decision**: Use a Panel with `AutoScroll = true` containing Label controls.

**Rationale**: This is the simplest WinForms approach - Panel natively supports scrolling when content exceeds bounds. No custom scrollbar implementation needed.

**Alternatives considered**:
- RichTextBox: Overkill for static text
- Custom scroll implementation: Violates simplicity principle

### 4. About Tab Layout Pattern

**Task**: Standard layout pattern for About dialogs in Windows applications.

**Decision**: Vertical layout with sections:
1. Application icon/name header
2. Version information
3. Developer/author attribution
4. "Third-Party Libraries" section header
5. List of libraries with license types

**Rationale**: This follows standard Windows application About dialog conventions, providing expected information hierarchy.

## Summary

No NEEDS CLARIFICATION items remain. All technical decisions align with constitution principles (simplicity, no new dependencies, Windows-native approach).
