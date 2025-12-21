# Tasks: Multi-Language Support

**Input**: Design documents from `/specs/006-multi-language/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests not explicitly requested - test tasks excluded.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/DataUsageReporter/`, `tests/DataUsageReporter.Tests/` at repository root
- Paths follow project structure from plan.md

---

## Phase 1: Setup

**Purpose**: Create localization infrastructure

- [x] T001 Create directory src/DataUsageReporter/Core/Localization/
- [x] T002 Create directory src/DataUsageReporter/Resources/
- [x] T003 [P] Create ILocalizationService interface in src/DataUsageReporter/Core/Localization/ILocalizationService.cs
- [x] T004 [P] Create SupportedLanguage record in src/DataUsageReporter/Core/Localization/SupportedLanguage.cs
- [x] T005 [P] Create LanguageChangedEventArgs in src/DataUsageReporter/Core/Localization/LanguageChangedEventArgs.cs

---

## Phase 2: Foundational (Resource Files)

**Purpose**: Create translation resource files - MUST complete before user stories

**⚠️ CRITICAL**: Resource files are needed by all user stories

- [x] T006 Create Strings.resx (English default) in src/DataUsageReporter/Resources/Strings.resx with all UI strings
- [x] T007 Create Strings.fr.resx (French) in src/DataUsageReporter/Resources/Strings.fr.resx with French translations
- [x] T008 Implement LocalizationService in src/DataUsageReporter/Core/Localization/LocalizationService.cs
- [x] T009 Update settings model to include Language property in src/DataUsageReporter/Data/ISettingsRepository.cs

**Checkpoint**: Localization infrastructure ready - user story implementation can begin

---

## Phase 3: User Story 1 - Select Application Language (Priority: P1) 🎯 MVP

**Goal**: User can select language in Settings, UI updates immediately, preference persists

**Independent Test**: Open Settings, select French, verify all text changes to French, restart app, verify French is still selected

### Implementation for User Story 1

- [x] T010 [US1] Add language dropdown to Settings tab in src/DataUsageReporter/UI/OptionsForm.cs
- [x] T011 [US1] Wire language dropdown to LocalizationService.SetLanguage() in src/DataUsageReporter/UI/OptionsForm.cs
- [x] T012 [US1] Implement RefreshStrings() method in src/DataUsageReporter/UI/OptionsForm.cs
- [x] T013 [US1] Subscribe to LanguageChanged event in src/DataUsageReporter/UI/OptionsForm.cs
- [x] T014 [US1] Update all hardcoded strings in src/DataUsageReporter/UI/OptionsForm.cs to use localized strings
- [x] T015 [US1] Update TrayIcon context menu to use localized strings in src/DataUsageReporter/UI/TrayIcon.cs
- [N/A] T016 [US1] SpeedOverlay uses arrows/formatted speeds - no localization needed
- [x] T017 [US1] Update GraphPanel title/labels to use localized strings in src/DataUsageReporter/UI/GraphPanel.cs
- [x] T018 [US1] Initialize LocalizationService in Program.cs and restore saved language preference
- [x] T019 [US1] Inject ILocalizationService into UI components via dependency injection in src/DataUsageReporter/Program.cs

**Checkpoint**: User Story 1 complete - UI language selection works and persists

---

## Phase 4: User Story 2 - Email Reports in Preferred Language (Priority: P2)

**Goal**: Email reports sent in user's preferred language

**Independent Test**: Set language to French, trigger email report, verify email content is in French

### Implementation for User Story 2

- [x] T020 [US2] Add email-specific strings to Strings.resx (Email_Subject, Email_Greeting, etc.)
- [x] T021 [US2] Add email-specific French strings to Strings.fr.resx
- [x] T022 [US2] Update ReportGenerator to use ILocalizationService in src/DataUsageReporter/Email/ReportGenerator.cs
- [x] T023 [US2] Replace hardcoded email text with localized strings in src/DataUsageReporter/Email/ReportGenerator.cs

**Checkpoint**: User Story 2 complete - emails sent in preferred language

---

## Phase 5: User Story 3 - Automatic Language Detection (Priority: P3)

**Goal**: First-run detects OS language and uses it if supported

**Independent Test**: Delete settings.json, set Windows to French, start app, verify French is displayed

### Implementation for User Story 3

- [x] T024 [US3] Implement OS language detection using CultureInfo.CurrentUICulture in src/DataUsageReporter/Core/Localization/LocalizationService.cs
- [x] T025 [US3] Update first-run logic to detect and set default language in src/DataUsageReporter/Program.cs
- [x] T026 [US3] Add fallback to English if OS language not supported in src/DataUsageReporter/Core/Localization/LocalizationService.cs

**Checkpoint**: User Story 3 complete - auto-detection works on first run

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [x] T027 [P] Verify all UI strings are extracted to resource files (no hardcoded text remaining)
- [x] T028 [P] Verify French translations are complete and accurate
- [x] T029 Build and verify no compilation errors
- [ ] T030 Run application and test all acceptance scenarios from spec.md
- [ ] T031 Run quickstart.md validation steps

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational - core language selection
- **User Story 2 (Phase 4)**: Depends on Foundational - can run parallel to US1
- **User Story 3 (Phase 5)**: Depends on Foundational - can run parallel to US1/US2
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - No dependencies on US1
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - No dependencies on US1/US2

### Parallel Opportunities

- T003, T004, T005 can run in parallel (different files)
- T006, T007 can run in parallel (different resource files)
- US1, US2, US3 can run in parallel after Foundational phase
- T027, T028 can run in parallel (verification tasks)

---

## Parallel Example: Setup Phase

```bash
# Launch interface and types together:
Task: "Create ILocalizationService interface"
Task: "Create SupportedLanguage record"
Task: "Create LanguageChangedEventArgs"
```

## Parallel Example: User Stories

```bash
# After Foundational phase, all stories can start:
Task: "User Story 1 - Language dropdown in Settings"
Task: "User Story 2 - Email localization"
Task: "User Story 3 - OS language detection"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (create infrastructure)
2. Complete Phase 2: Foundational (resource files + service)
3. Complete Phase 3: User Story 1 (language selection in UI)
4. **STOP and VALIDATE**: Test language switching works
5. Deploy/demo if ready - users can select language

### Incremental Delivery

1. Complete Setup + Foundational → Localization infrastructure ready
2. Add User Story 1 → Language selection in Settings → Validate
3. Add User Story 2 → Localized emails → Validate
4. Add User Story 3 → Auto-detect OS language → Validate
5. Complete Polish → Full feature validation

---

## String Keys Reference

### UI Strings (Strings.resx / Strings.fr.resx)

| Key | English | French |
|-----|---------|--------|
| Tab_Usage | Usage | Utilisation |
| Tab_Graph | Graph | Graphique |
| Tab_Settings | Settings | Paramètres |
| Tab_Credits | Credits | Crédits |
| Label_Download | Download | Téléchargement |
| Label_Upload | Upload | Téléversement |
| Label_Language | Language | Langue |
| Button_Save | Save | Enregistrer |
| Button_Cancel | Cancel | Annuler |
| Menu_Options | Options | Options |
| Menu_Exit | Exit | Quitter |
| Graph_Title | Network Usage | Utilisation réseau |
| Graph_NoData | No data available | Aucune donnée disponible |

### Email Strings

| Key | English | French |
|-----|---------|--------|
| Email_Subject | Daily Usage Report | Rapport d'utilisation quotidien |
| Email_Greeting | Hello, | Bonjour, |
| Email_Downloaded | Downloaded | Téléchargé |
| Email_Uploaded | Uploaded | Téléversé |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- All three user stories can proceed in parallel after Foundational phase
- MVP is User Story 1 - language selection in Settings
- Total strings to translate: ~30 UI + ~10 email = ~40 strings
