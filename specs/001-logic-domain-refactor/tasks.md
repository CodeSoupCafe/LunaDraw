# Tasks: Logic Folder Domain-Based Reorganization

**Input**: Design documents from `/specs/001-logic-domain-refactor/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Not required - this is a pure structural refactoring. Validation is via existing test suite (all tests must pass without logic changes).

**Organization**: Tasks are grouped by user story to enable independent implementation and validation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Single .NET MAUI project at repository root
- All refactoring within `Logic/` folder
- Tests in `tests/LunaDraw.Tests/`

---

## Phase 1: Setup (Pre-Refactoring Preparation)

**Purpose**: Establish baseline and safety nets before structural changes

- [ ] T001 Create baseline git tag: `git tag baseline-pre-refactor`
- [ ] T002 Run full test suite and document pass/fail count in `specs/001-logic-domain-refactor/baseline-tests.txt`
- [ ] T003 Build project successfully: `dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0`
- [ ] T004 [P] Benchmark LoadDrawingAsync performance (10 runs, record median) in `specs/001-logic-domain-refactor/baseline-performance.md`
- [ ] T005 [P] Benchmark GenerateThumbnailAsync performance (10 runs, record median) in `specs/001-logic-domain-refactor/baseline-performance.md`
- [ ] T006 Review contracts/refactoring-contract.md and confirm all pre-refactoring checklist items

**Checkpoint**: Baseline established - refactoring can now proceed safely

---

## Phase 2: Foundational (Domain Folder Creation)

**Purpose**: Create target domain folder structure (no file moves yet - just empty folders)

**⚠️ CRITICAL**: These folders must exist before ANY file relocation in Phase 3+

- [ ] T007 Create `Logic/Storage/` directory
- [ ] T008 [P] Create `Logic/Input/` directory
- [ ] T009 [P] Create `Logic/Canvas/` directory
- [ ] T010 [P] Create `Logic/History/` directory
- [ ] T011 [P] Create `Logic/Playback/` directory
- [ ] T012 [P] Create `Logic/Selection/` directory
- [ ] T013 [P] Create `Logic/Layers/` directory
- [ ] T014 Verify all 7 domain folders created successfully

**Checkpoint**: Domain structure ready - file relocations can now begin

---

## Phase 3: User Story 1 - Locate Code by Domain (Priority: P1) 🎯 MVP

**Goal**: Reorganize code into domain-based folders so developers can find code by what it does, not how it's classified

**Independent Test**: Navigate Logic folder and confirm all code is in domain folders (Storage, Input, Canvas, History, Playback, Selection, Layers). Verify Handlers/ and Utils/ folders no longer exist.

### Simple Relocations (Well-Factored Files Under 200 Lines)

These files are relocated as-is with namespace updates only.

#### Storage Domain Relocations

- [ ] T015 [P] [US1] `git mv Logic/Utils/ClipboardMemento.cs Logic/Storage/ClipboardMemento.cs` (45 lines)
- [ ] T016 [P] [US1] Update namespace in `Logic/Storage/ClipboardMemento.cs` to `LunaDraw.Logic.Storage`
- [ ] T017 [P] [US1] `git mv Logic/Utils/ThumbnailCacheFacade.cs Logic/Storage/ThumbnailCacheFacade.cs` (93 lines)
- [ ] T018 [P] [US1] Update namespace in `Logic/Storage/ThumbnailCacheFacade.cs` to `LunaDraw.Logic.Storage`
- [ ] T019 [P] [US1] `git mv Logic/Utils/IThumbnailCacheFacade.cs Logic/Storage/IThumbnailCacheFacade.cs` (20 lines)
- [ ] T020 [P] [US1] Update namespace in `Logic/Storage/IThumbnailCacheFacade.cs` to `LunaDraw.Logic.Storage`

#### Input Domain Relocations

- [ ] T021 [P] [US1] `git mv Logic/Handlers/CanvasInputHandler.cs Logic/Input/CanvasInputHandler.cs` (388 lines - under 400 threshold)
- [ ] T022 [P] [US1] Update namespace in `Logic/Input/CanvasInputHandler.cs` to `LunaDraw.Logic.Input`
- [ ] T023 [P] [US1] `git mv Logic/Handlers/ICanvasInputHandler.cs Logic/Input/ICanvasInputHandler.cs` (34 lines)
- [ ] T024 [P] [US1] Update namespace in `Logic/Input/ICanvasInputHandler.cs` to `LunaDraw.Logic.Input`

#### Canvas Domain Relocations

- [ ] T025 [P] [US1] `git mv Logic/Utils/BitmapCache.cs Logic/Canvas/BitmapCache.cs` (105 lines)
- [ ] T026 [P] [US1] Update namespace in `Logic/Canvas/BitmapCache.cs` to `LunaDraw.Logic.Canvas`

#### History Domain Relocations

- [ ] T027 [P] [US1] `git mv Logic/Utils/HistoryMemento.cs Logic/History/HistoryMemento.cs` (86 lines)
- [ ] T028 [P] [US1] Update namespace in `Logic/History/HistoryMemento.cs` to `LunaDraw.Logic.History`

#### Playback Domain Relocations

- [ ] T029 [P] [US1] `git mv Logic/Handlers/PlaybackHandler.cs Logic/Playback/PlaybackHandler.cs` (215 lines)
- [ ] T030 [P] [US1] Update namespace in `Logic/Playback/PlaybackHandler.cs` to `LunaDraw.Logic.Playback`
- [ ] T031 [P] [US1] `git mv Logic/Handlers/IPlaybackHandler.cs Logic/Playback/IPlaybackHandler.cs` (62 lines)
- [ ] T032 [P] [US1] Update namespace in `Logic/Playback/IPlaybackHandler.cs` to `LunaDraw.Logic.Playback`
- [ ] T033 [P] [US1] `git mv Logic/Handlers/RecordingHandler.cs Logic/Playback/RecordingHandler.cs` (39 lines)
- [ ] T034 [P] [US1] Update namespace in `Logic/Playback/RecordingHandler.cs` to `LunaDraw.Logic.Playback`
- [ ] T035 [P] [US1] `git mv Logic/Handlers/IRecordingHandler.cs Logic/Playback/IRecordingHandler.cs` (38 lines)
- [ ] T036 [P] [US1] Update namespace in `Logic/Playback/IRecordingHandler.cs` to `LunaDraw.Logic.Playback`

#### Selection Domain Relocations

- [ ] T037 [P] [US1] `git mv Logic/Utils/SelectionObserver.cs Logic/Selection/SelectionObserver.cs` (142 lines)
- [ ] T038 [P] [US1] Update namespace in `Logic/Selection/SelectionObserver.cs` to `LunaDraw.Logic.Selection`

#### Layers Domain Relocations

- [ ] T039 [P] [US1] `git mv Logic/Utils/LayerFacade.cs Logic/Layers/LayerFacade.cs` (177 lines)
- [ ] T040 [P] [US1] Update namespace in `Logic/Layers/LayerFacade.cs` to `LunaDraw.Logic.Layers`
- [ ] T041 [P] [US1] `git mv Logic/Utils/ILayerFacade.cs Logic/Layers/ILayerFacade.cs` (41 lines)
- [ ] T042 [P] [US1] Update namespace in `Logic/Layers/ILayerFacade.cs` to `LunaDraw.Logic.Layers`
- [ ] T043 [P] [US1] `git mv Logic/Utils/QuadTreeMemento.cs Logic/Layers/QuadTreeMemento.cs` (171 lines)
- [ ] T044 [P] [US1] Update namespace in `Logic/Layers/QuadTreeMemento.cs` to `LunaDraw.Logic.Layers`
- [ ] T045 [P] [US1] `git mv Logic/Utils/PreferencesFacade.cs Logic/Layers/PreferencesFacade.cs` (71 lines)
- [ ] T046 [P] [US1] Update namespace in `Logic/Layers/PreferencesFacade.cs` to `LunaDraw.Logic.Layers`
- [ ] T047 [P] [US1] `git mv Logic/Utils/IPreferencesFacade.cs Logic/Layers/IPreferencesFacade.cs` (36 lines)
- [ ] T048 [P] [US1] Update namespace in `Logic/Layers/IPreferencesFacade.cs` to `LunaDraw.Logic.Layers`

### Import Statement Updates for Relocated Files

- [ ] T049 [US1] Update ViewModels imports: Replace `using LunaDraw.Logic.Handlers;` and `using LunaDraw.Logic.Utils;` with domain-specific imports in all files under `Logic/ViewModels/`
- [ ] T050 [US1] Update Pages imports: Replace Handlers/Utils imports with domain imports in `Pages/MainPage.xaml.cs` and `Pages/PlaybackPage.xaml.cs`
- [ ] T051 [US1] Update MauiProgram.cs imports: Add domain namespace imports (`using LunaDraw.Logic.Storage;`, etc.) and remove old Handlers/Utils imports

### Validation for User Story 1 (Simple Relocations Complete)

- [ ] T052 [US1] Run `dotnet build` - expect success (relocated files only, no decomposition yet)
- [ ] T053 [US1] Run `dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj` - expect 100% pass rate
- [ ] T054 [US1] Verify `git log --follow Logic/Input/CanvasInputHandler.cs` shows full history (spot-check git mv preservation)

**Checkpoint**: US1 simple relocations complete - all well-factored files now in domain folders

---

## Phase 4: User Story 2 - Understand Single Responsibility Classes (Priority: P2)

**Goal**: Decompose large classes (400+ lines) into focused, single-responsibility units

**Independent Test**: Review file line counts - confirm DrawingStorageMomento and DrawingThumbnailHandler no longer exist as monolithic files. Verify decomposed handlers exist in Storage and Canvas domains.

### Decompose DrawingStorageMomento (541 lines) → Storage Domain

**Strategy**: Extract each operation into its own handler class, preserving all functionality.

#### Create Shared Configuration

- [ ] T055 [US2] Create `Logic/Storage/DrawingStorageConfiguration.cs` (~40 lines) - Extract storage path, JSON options, file lock from DrawingStorageMomento constructor

#### Create Operation Handlers

- [ ] T056 [P] [US2] Create `Logic/Storage/LoadAllDrawingsHandler.cs` (~40 lines) - Extract LoadAllDrawingsAsync() logic
- [ ] T057 [P] [US2] Create `Logic/Storage/LoadDrawingHandler.cs` (~30 lines) - Extract LoadDrawingAsync() logic
- [ ] T058 [P] [US2] Create `Logic/Storage/SaveDrawingHandler.cs` (~45 lines) - Extract ExternalDrawingAsync() and GetNextDefaultNameAsync() logic
- [ ] T059 [P] [US2] Create `Logic/Storage/DeleteDrawingHandler.cs` (~25 lines) - Extract DeleteDrawingAsync() logic
- [ ] T060 [P] [US2] Create `Logic/Storage/DuplicateDrawingHandler.cs` (~35 lines) - Extract DuplicateDrawingAsync() logic
- [ ] T061 [P] [US2] Create `Logic/Storage/RenameDrawingHandler.cs` (~50 lines) - Extract RenameDrawingAsync() and RenameUntitledDrawingsAsync() logic

#### Create Conversion Helper

- [ ] T062 [US2] Create `Logic/Storage/DrawingConverter.cs` (~350 lines) - Extract CreateExternalDrawingFromCurrent(), RestoreLayers(), GetBrushShapeStatic() logic

#### Create New Interface and Facade

- [ ] T063 [US2] Create `Logic/Storage/IDrawingStorage.cs` (~30 lines) - Redesigned interface (replaces IDrawingStorageMomento)
- [ ] T064 [US2] Create `Logic/Storage/DrawingStorageFacade.cs` (~100 lines) - Facade that composes all handlers and implements IDrawingStorage

#### Update DI Registration for DrawingStorage

- [ ] T065 [US2] Update `MauiProgram.cs` - Replace `builder.Services.AddSingleton<IDrawingStorageMomento, DrawingStorageMomento>()` with `builder.Services.AddSingleton<IDrawingStorage, DrawingStorageFacade>()`
- [ ] T066 [US2] Update all consumer code - Replace `IDrawingStorageMomento` references with `IDrawingStorage` in ViewModels and Pages

#### Delete Original DrawingStorageMomento

- [ ] T067 [US2] Delete `Logic/Utils/DrawingStorageMomento.cs` (original 541-line file no longer needed)

### Decompose DrawingThumbnailHandler (485 lines) → Canvas Domain

**Strategy**: Separate retrieval/caching from generation, extract rendering to extension methods.

#### Create Thumbnail Provider and Generator

- [ ] T068 [P] [US2] Create `Logic/Canvas/IThumbnailProvider.cs` (~25 lines) - New interface (replaces IDrawingThumbnailHandler)
- [ ] T069 [P] [US2] Create `Logic/Canvas/ThumbnailProvider.cs` (~120 lines) - Extract GetThumbnail*(), InvalidateThumbnailAsync(), ClearCacheAsync() with in-memory caching
- [ ] T070 [P] [US2] Create `Logic/Canvas/ThumbnailGenerator.cs` (~150 lines) - Extract GenerateThumbnail*(), RenderElement(), RenderStamps() logic

#### Update DI Registration for Thumbnails

- [ ] T071 [US2] Update `MauiProgram.cs` - Replace `IDrawingThumbnailHandler` with `IThumbnailProvider` registration
- [ ] T072 [US2] Update consumer code - Replace `IDrawingThumbnailHandler` references with `IThumbnailProvider` in ViewModels

#### Delete Original DrawingThumbnailHandler

- [ ] T073 [US2] Delete `Logic/Handlers/DrawingThumbnailHandler.cs` (original 485-line file no longer needed)
- [ ] T074 [US2] Delete `Logic/Handlers/IDrawingThumbnailHandler.cs` (old interface replaced by IThumbnailProvider)

### Validation for User Story 2 (Decompositions Complete)

- [ ] T075 [US2] Run `dotnet build` - expect success (all decomposed files compile)
- [ ] T076 [US2] Run `dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj` - expect 100% pass rate
- [ ] T077 [US2] Verify no files in Logic/Storage or Logic/Canvas exceed 400 lines (except DrawingConverter at ~350 lines, which is acceptable)

**Checkpoint**: US2 decomposition complete - all large classes broken into focused units

---

## Phase 5: User Story 3 - Reuse Static Utilities via Extensions (Priority: P3)

**Goal**: Convert reusable rendering utilities to extension methods for better discoverability and API fluency

**Independent Test**: Verify rendering methods exist as extension methods in SkiaSharpExtensions. Test that call sites can use fluent API (e.g., `externalPath.Render(canvas, paint)`).

### Extract Rendering Methods to SkiaSharpExtensions

- [ ] T078 [P] [US3] Extract RenderPath() from DrawingThumbnailHandler (before deletion) to `Logic/Extensions/SkiaSharpExtensions.cs` as extension method `public static void Render(this External.Path pathElement, SKCanvas canvas, SKPaint paint)` (~30 lines)
- [ ] T079 [P] [US3] Extract RenderRectangle() to SkiaSharpExtensions as `public static void Render(this External.Rectangle rectangleElement, SKCanvas canvas, SKPaint paint)` (~42 lines)
- [ ] T080 [P] [US3] Extract RenderEllipse() to SkiaSharpExtensions as `public static void Render(this External.Ellipse ellipseElement, SKCanvas canvas, SKPaint paint)` (~40 lines)
- [ ] T081 [P] [US3] Extract RenderLine() to SkiaSharpExtensions as `public static void Render(this External.Line lineElement, SKCanvas canvas, SKPaint paint)` (~33 lines)
- [ ] T082 [P] [US3] Extract GetShapePath() to SkiaSharpExtensions as `public static SKPath? ToSkiaPath(this BrushShapeType shapeType)` (~40 lines)

### Update Call Sites to Use Extension Methods

- [ ] T083 [US3] Update `Logic/Canvas/ThumbnailGenerator.cs` to use extension methods instead of local rendering methods (replace direct calls with `element.Render(canvas, paint)`)
- [ ] T084 [US3] Add `using LunaDraw.Logic.Extensions;` to ThumbnailGenerator.cs and any other files using the rendering extensions

### Validation for User Story 3 (Extensions Complete)

- [ ] T085 [US3] Run `dotnet build` - expect success (extension methods compile and are discoverable)
- [ ] T086 [US3] Run `dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj` - expect 100% pass rate
- [ ] T087 [US3] Verify `Logic/Extensions/SkiaSharpExtensions.cs` contains all 5 rendering extension methods

**Checkpoint**: US3 extension methods complete - rendering utilities now fluent and discoverable

---

## Phase 6: Polish & Final Cleanup

**Purpose**: Complete the refactoring with cleanup and validation

### Delete Empty Folders

- [ ] T088 Verify `Logic/Handlers/` is empty (all files moved or deleted)
- [ ] T089 Delete `Logic/Handlers/` directory
- [ ] T090 Verify `Logic/Utils/` is empty (all files moved or deleted)
- [ ] T091 Delete `Logic/Utils/` directory

### Final Namespace and Import Cleanup

- [ ] T092 Search codebase for remaining `using LunaDraw.Logic.Handlers;` or `using LunaDraw.Logic.Utils;` - remove or replace with domain imports
- [ ] T093 Update `CLAUDE.md` - Replace references to Handlers/Utils folder structure with domain folder structure

### Test Import Updates

- [ ] T094 [P] Update test file imports in `tests/LunaDraw.Tests/` - replace Handlers/Utils namespaces with domain namespaces (Storage, Input, Canvas, etc.)
- [ ] T095 [P] Update mock objects in tests - rename mocks from `IDrawingStorageMomento` to `IDrawingStorage`, `IDrawingThumbnailHandler` to `IThumbnailProvider`

### Final Validation

- [ ] T096 Run full test suite: `dotnet test tests/LunaDraw.Tests/LunaDraw.Tests.csproj --logger "console;verbosity=detailed"` - confirm 100% pass rate (same count as baseline)
- [ ] T097 Run full build: `dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0` - confirm zero errors, zero warnings
- [ ] T098 Launch application and perform smoke test (create, save, load, delete drawing, generate thumbnails, undo/redo, playback)
- [ ] T099 Re-benchmark LoadDrawingAsync and GenerateThumbnailAsync - confirm performance within ±5% of baseline
- [ ] T100 Run validation script: `powershell.exe -ExecutionPolicy Bypass -File "specs/001-logic-domain-refactor/contracts/validate-refactoring.ps1"` (if script created)

### Documentation

- [ ] T101 [P] Create commit with message: "Refactor: Reorganize Logic folder into domain-based structure" with detailed description listing all relocated/decomposed files
- [ ] T102 [P] Update `specs/001-logic-domain-refactor/quickstart.md` if any deviations from plan occurred
- [ ] T103 [P] Document any exceptions (files exceeding 400 lines with justification) in `specs/001-logic-domain-refactor/exceptions.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup (T001-T006) completion - domain folders must exist before relocations
- **User Story 1 (Phase 3)**: Depends on Foundational (T007-T014) - requires domain folders to exist
- **User Story 2 (Phase 4)**: Depends on US1 (T015-T054) completion - relocations must be done before decomposition to avoid path conflicts
- **User Story 3 (Phase 5)**: Depends on US2 (T055-T077) completion - need ThumbnailGenerator to exist before extracting its rendering methods
- **Polish (Phase 6)**: Depends on all user stories (T015-T087) being complete

### User Story Dependencies

- **User Story 1 (P1)**: Simple relocations - can start after Foundational phase
- **User Story 2 (P2)**: Large class decomposition - MUST complete US1 first (to avoid moving files that will be deleted)
- **User Story 3 (P3)**: Extension method extraction - MUST complete US2 first (to extract from decomposed ThumbnailGenerator)

**Critical Ordering**: US1 → US2 → US3 (sequential, not parallel) due to file dependencies

### Within Each User Story

**US1 Relocations**:
- All `git mv` commands can run in parallel by domain (Storage relocations || Input relocations || Canvas relocations, etc.)
- Namespace updates can run in parallel with their corresponding relocations (each file independently)
- Import statement updates (T049-T051) must wait for all relocations to complete

**US2 Decompositions**:
- DrawingStorageMomento handlers (T055-T062) can be created in parallel
- DrawingThumbnailHandler components (T068-T070) can be created in parallel
- Facade creation and DI updates must wait for handlers to exist
- Original file deletion must be last

**US3 Extensions**:
- All extension method extractions (T078-T082) can run in parallel (different methods)
- Call site updates must wait for all extensions to exist

### Parallel Opportunities

- **Setup Phase**: T004 || T005 (different benchmarks)
- **Foundational Phase**: T008 || T009 || T010 || T011 || T012 || T013 (different domain folders)
- **US1 Relocations by Domain**: All relocations within a domain can run in parallel:
  - Storage: T015 || T017 || T019
  - Input: T021 || T023
  - Canvas: T025
  - History: T027
  - Playback: T029 || T031 || T033 || T035
  - Selection: T037
  - Layers: T039 || T041 || T043 || T045 || T047
- **US2 Storage Handlers**: T056 || T057 || T058 || T059 || T060 || T061 (different files)
- **US2 Thumbnail Components**: T068 || T069 || T070 (different files)
- **US3 Extension Extractions**: T078 || T079 || T080 || T081 || T082 (different methods)
- **Polish Tests**: T094 || T095 (different test concerns)
- **Polish Docs**: T101 || T102 || T103 (different documents)

---

## Parallel Example: User Story 1 (Relocations)

Within US1, many file relocations can happen in parallel since they operate on different files:

```bash
# Storage domain relocations (parallel)
Task T015: "git mv Logic/Utils/ClipboardMemento.cs Logic/Storage/"
Task T017: "git mv Logic/Utils/ThumbnailCacheFacade.cs Logic/Storage/"
Task T019: "git mv Logic/Utils/IThumbnailCacheFacade.cs Logic/Storage/"

# Immediately after, namespace updates (parallel)
Task T016: "Update namespace in Logic/Storage/ClipboardMemento.cs"
Task T018: "Update namespace in Logic/Storage/ThumbnailCacheFacade.cs"
Task T020: "Update namespace in Logic/Storage/IThumbnailCacheFacade.cs"
```

---

## Parallel Example: User Story 2 (Decompositions)

Within US2, handler creation can happen in parallel:

```bash
# DrawingStorageMomento decomposition (parallel handler creation)
Task T056: "Create Logic/Storage/LoadAllDrawingsHandler.cs"
Task T057: "Create Logic/Storage/LoadDrawingHandler.cs"
Task T058: "Create Logic/Storage/SaveDrawingHandler.cs"
Task T059: "Create Logic/Storage/DeleteDrawingHandler.cs"
Task T060: "Create Logic/Storage/DuplicateDrawingHandler.cs"
Task T061: "Create Logic/Storage/RenameDrawingHandler.cs"
```

---

## Implementation Strategy

### Sequential Execution (Recommended for Solo Developer)

This refactoring MUST be done sequentially due to file dependencies:

1. **Phase 1: Setup** (T001-T006) - Establish baseline
2. **Phase 2: Foundational** (T007-T014) - Create folders
3. **Phase 3: User Story 1** (T015-T054) - Complete ALL relocations
4. **Checkpoint**: Verify all tests pass, all files relocated
5. **Phase 4: User Story 2** (T055-T077) - Complete ALL decompositions
6. **Checkpoint**: Verify all tests pass, large classes decomposed
7. **Phase 5: User Story 3** (T078-T087) - Complete ALL extension extractions
8. **Checkpoint**: Verify all tests pass, extensions work
9. **Phase 6: Polish** (T088-T103) - Final cleanup and validation

### Within-Phase Parallelization

While phases must be sequential, tasks WITHIN each phase marked [P] can run in parallel if you have automation or team capacity.

### Validation Strategy

- **After Phase 1**: Baseline documented
- **After Phase 2**: Folders exist
- **After Phase 3 (US1)**: Tests pass, git history preserved
- **After Phase 4 (US2)**: Tests pass, no 400+ line files
- **After Phase 5 (US3)**: Tests pass, extensions work
- **After Phase 6**: Full contract validation (contracts/refactoring-contract.md)

### Rollback Points

- **Before Phase 3**: `git reset --hard baseline-pre-refactor`
- **After Phase 3**: `git reset --hard` to commit after T054
- **After Phase 4**: `git reset --hard` to commit after T077
- **After Phase 5**: `git reset --hard` to commit after T087

---

## Notes

- **[P] tasks** = different files, no dependencies within phase
- **[Story] label** maps task to specific user story for traceability
- **Sequential user stories** (US1 → US2 → US3) due to file dependencies
- **Git history preservation**: ALL relocations use `git mv`, never delete+create
- **Zero functional changes**: Pure structural refactoring only
- **Test validation**: 100% pass rate required after EVERY phase
- **Commit frequently**: After each logical group (e.g., after all Storage relocations)
- **Stop at checkpoints**: Validate independently before proceeding

---

## Task Summary

- **Total Tasks**: 103
- **Setup Phase**: 6 tasks
- **Foundational Phase**: 8 tasks
- **User Story 1 (P1)**: 40 tasks (relocations + imports + validation)
- **User Story 2 (P2)**: 23 tasks (decompositions + DI updates + validation)
- **User Story 3 (P3)**: 10 tasks (extension methods + validation)
- **Polish Phase**: 16 tasks (cleanup + final validation + docs)

**Parallel Opportunities**: 58 tasks marked [P] can run concurrently within their phase
**Sequential Constraint**: User stories MUST run in order (US1 → US2 → US3) due to file dependencies

**Estimated Effort**: 2-3 days for solo developer (careful, methodical execution with validation at each checkpoint)
