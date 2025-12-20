# Contracts: About Tab

**Feature**: 002-credits-tab
**Date**: 2025-12-20

## Overview

This feature does not introduce new public interfaces or APIs. It is a UI-only addition to the existing OptionsForm.

## UI Contract

### About Tab Content Structure

The About tab will display the following sections in order:

```
┌─────────────────────────────────────┐
│ Data Usage Reporter                 │  <- Application name (bold, larger)
│ Version X.X.X                       │  <- Version from assembly
│                                     │
│ Created by [Author]                 │  <- Developer attribution
│                                     │
│ ─────────────────────────────────── │
│ Third-Party Libraries               │  <- Section header
│                                     │
│ • MailKit                           │
│   MIT License                       │
│                                     │
│ • DnsClient                         │
│   Apache 2.0                        │
│                                     │
│ • ScottPlot                         │
│   MIT License                       │
│                                     │
│ • Microsoft.Data.Sqlite             │
│   MIT License                       │
│                                     │
│ • Vanara.PInvoke.IpHlpApi          │
│   MIT License                       │
└─────────────────────────────────────┘
```

## Integration Points

- **OptionsForm.cs**: Add new TabPage to existing TabControl
- **No new files required**: All implementation within existing class
