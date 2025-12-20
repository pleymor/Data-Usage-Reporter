# Data Model: About Tab

**Feature**: 002-credits-tab
**Date**: 2025-12-20

## Overview

This feature does not introduce persistent data storage. All data is static and embedded in the source code.

## Entities

### Application Info (Static)

Represents the application's metadata, retrieved at runtime from assembly attributes.

| Field | Type | Source |
|-------|------|--------|
| Name | string | Assembly Product attribute |
| Version | string | Assembly Version |
| Author | string | Hardcoded or Assembly Company |
| Description | string | Assembly Description |

### Library Credit (Static)

Represents a third-party library attribution entry.

| Field | Type | Value |
|-------|------|-------|
| Name | string | Library name |
| License | string | License type (MIT, Apache 2.0, etc.) |

## Static Data

The following libraries will be listed in the About tab:

```
MailKit - MIT License
DnsClient - Apache 2.0
ScottPlot - MIT License
Microsoft.Data.Sqlite - MIT License
Vanara.PInvoke.IpHlpApi - MIT License
```

## State Transitions

N/A - No state management required.

## Validation Rules

N/A - Static content requires no validation.
