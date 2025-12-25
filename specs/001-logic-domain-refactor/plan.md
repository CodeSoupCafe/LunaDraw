# Implementation Plan: Logic Folder Domain-Based Reorganization

**Branch**: `001-logic-domain-refactor` | **Date**: 2025-12-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-logic-domain-refactor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature reorganizes the Logic folder structure from a technical classification system (Handlers, Utils) to a domain-based organization (Storage, Input, Canvas, History, Playback, Selection, Layers). Large classes exceeding 400 lines will be decomposed into single-responsibility units following the Single Responsibility Principle. Reusable utility methods will be converted to extension methods where appropriate. This is a pure structural refactoring with zero functional changes - all existing tests must pass without modification to test logic.

**Primary Goal**: Improve code discoverability and maintainability by organizing code by domain responsibility rather than technical pattern.

**Technical Approach**: Use `git mv` to preserve history, decompose large classes into focused operation handlers, convert appropriate utilities to extension methods, update namespaces and DI registration, verify with existing test suite.

## Technical Context

**Language/Version**: C# 13, .NET 10 (.NET MAUI)
**Primary Dependencies**: .NET MAUI, SkiaSharp, ReactiveUI, CommunityToolkit.Maui
**Storage**: File system (JSON serialization for drawing storage)
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Windows 10.0.19041.0, Android 36.0, iOS 26.0, MacCatalyst 26.0
**Project Type**: Single project (MAUI cross-platform application)
**Performance Goals**: No performance degradation (NFR-001); maintain current rendering performance (60 fps target)
**Constraints**: Zero functional changes allowed; 100% test pass rate required; preserve git history via `git mv`
**Scale/Scope**: ~20 files to reorganize (Handlers + Utils folders); 3 large files (400+ lines) to decompose; ~2,800 total lines of code affected

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Constitution Compliance

✅ **I. Child-Centric UX**: Not applicable - no UI changes in this refactoring

✅ **II. Reactive Architecture**: Compliant
- All reactive patterns (IMessageBus, ReactiveObject, observables) remain unchanged
- Namespace updates only; no architectural changes

✅ **III. Test-First & Quality**: Compliant
- All existing tests must pass without logic modification (FR-012)
- Test naming conventions preserved
- No new functionality added (out of scope)

✅ **IV. SOLID & Clean Code**: **PRIMARY FOCUS**
- **Single Responsibility Principle**: Decomposing 400+ line classes into focused units (FR-004, FR-005)
- **No abbreviations**: File naming follows full descriptive names (FR-014)
- **No underscores/regions**: Existing code compliance; preserved in refactoring
- **Static Extensions**: Moving appropriate utilities to extension methods (FR-007, FR-008)
- **Refactoring to clean state**: Eliminating generic Handlers/Utils folders (FR-010)

✅ **V. SkiaSharp & Performance**: Compliant
- No changes to rendering logic
- Performance requirement: no degradation (NFR-001)

⚠️ **VI. Architecture Patterns & Naming**: **IMPROVEMENT TARGET**
- **Current violation**: Generic "Handler" and "Utils" folders violate domain-driven modularity
- **Resolution**: Reorganizing into domain folders (Storage, Input, Canvas, History, Playback, Selection, Layers)
- **Pattern preservation**: Memento and Facade patterns preserved (FR-006)
- **"Services" ban**: No Services folders exist or will be created (FR-010)

✅ **VII. SPARC Methodology**: Compliant
- Specification created (spec.md)
- Implementation plan (this document)
- Refactoring follows simplicity principle (minimize complexity)

### Gate Status

**PASS** - Proceeding to Phase 0

**Justification**: This refactoring directly addresses Constitution VI violations by eliminating generic technical folders in favor of domain-based organization. It enhances SOLID compliance (Constitution IV) by decomposing large classes. No new violations introduced.

## Project Structure

### Documentation (this feature)

```text
specs/001-logic-domain-refactor/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification
├── research.md          # Phase 0 output - domain mapping decisions
├── data-model.md        # Phase 1 output - refactored file structure map
├── quickstart.md        # Phase 1 output - migration guide for developers
└── checklists/
    └── requirements.md  # Specification quality checklist
```

### Source Code (repository root)

**Current Structure** (to be eliminated):

```text
Logic/
├── Handlers/                    # TO BE ELIMINATED
│   ├── CanvasInputHandler.cs          # 388 lines - needs decomposition?
│   ├── DrawingThumbnailHandler.cs     # 485 lines - DECOMPOSE
│   ├── ICanvasInputHandler.cs
│   ├── IDrawingThumbnailHandler.cs
│   ├── IPlaybackHandler.cs
│   ├── IRecordingHandler.cs
│   ├── PlaybackHandler.cs             # 215 lines
│   └── RecordingHandler.cs            # 39 lines
├── Utils/                       # TO BE ELIMINATED
│   ├── BitmapCache.cs                 # 105 lines
│   ├── ClipboardMemento.cs            # 45 lines
│   ├── DrawingStorageMomento.cs       # 541 lines - DECOMPOSE
│   ├── HistoryMemento.cs              # 86 lines
│   ├── ILayerFacade.cs
│   ├── IPreferencesFacade.cs
│   ├── IThumbnailCacheFacade.cs
│   ├── LayerFacade.cs                 # 177 lines
│   ├── PreferencesFacade.cs           # 71 lines
│   ├── QuadTreeMemento.cs             # 171 lines
│   ├── SelectionObserver.cs           # 142 lines
│   └── ThumbnailCacheFacade.cs        # 93 lines
├── Constants/                   # UNCHANGED
├── Extensions/                  # ENHANCED (new extension methods added)
├── Messages/                    # UNCHANGED
├── Models/                      # UNCHANGED
├── Tools/                       # UNCHANGED
└── ViewModels/                  # UNCHANGED (only namespace imports updated)
```

**Target Structure** (domain-based):

```text
Logic/
├── Storage/                     # NEW - Drawing persistence domain
│   ├── DrawingStorage.cs               # Interface (extracted from DrawingStorageMomento)
│   ├── LoadAllDrawingsHandler.cs       # Single operation (from DrawingStorageMomento)
│   ├── LoadDrawingHandler.cs           # Single operation
│   ├── SaveDrawingHandler.cs           # Single operation (ExternalDrawingAsync)
│   ├── DeleteDrawingHandler.cs         # Single operation
│   ├── DuplicateDrawingHandler.cs      # Single operation
│   ├── RenameDrawingHandler.cs         # Single operation
│   ├── DrawingConverter.cs             # Conversion helpers (from DrawingStorageMomento)
│   ├── ClipboardMemento.cs             # Relocated as-is (45 lines)
│   ├── ThumbnailCacheFacade.cs         # Relocated as-is (93 lines)
│   └── IThumbnailCacheFacade.cs        # Interface
├── Input/                       # NEW - Canvas input handling domain
│   ├── CanvasInputHandler.cs           # Relocated/decomposed (388 lines - evaluate)
│   ├── ICanvasInputHandler.cs          # Interface
│   ├── TouchTracker.cs                 # Extracted if CanvasInputHandler > 400 lines after review
│   └── GestureProcessor.cs             # Extracted if needed
├── Canvas/                      # NEW - Canvas rendering/bitmap domain
│   ├── BitmapCache.cs                  # Relocated as-is (105 lines)
│   ├── ThumbnailGenerator.cs           # Thumbnail generation logic (from DrawingThumbnailHandler)
│   ├── ThumbnailProvider.cs            # Thumbnail retrieval/caching (from DrawingThumbnailHandler)
│   └── IThumbnailProvider.cs           # Interface (replaces IDrawingThumbnailHandler)
├── History/                     # NEW - Undo/redo domain
│   ├── HistoryMemento.cs               # Relocated as-is (86 lines)
│   └── HistoryStack.cs                 # (Future potential extraction if HistoryMemento grows)
├── Playback/                    # NEW - Movie mode recording/playback domain
│   ├── PlaybackHandler.cs              # Relocated as-is (215 lines)
│   ├── IPlaybackHandler.cs             # Interface
│   ├── RecordingHandler.cs             # Relocated as-is (39 lines)
│   └── IRecordingHandler.cs            # Interface
├── Selection/                   # NEW - Selection tracking domain
│   ├── SelectionObserver.cs            # Relocated as-is (142 lines)
│   └── SelectionState.cs               # (Future potential extraction if SelectionObserver grows)
├── Layers/                      # NEW - Layer management domain
│   ├── LayerFacade.cs                  # Relocated as-is (177 lines)
│   ├── ILayerFacade.cs                 # Interface
│   ├── QuadTreeMemento.cs              # Relocated as-is (171 lines - spatial indexing for layers)
│   └── PreferencesFacade.cs            # Relocated (71 lines - layer preferences)
│       └── IPreferencesFacade.cs       # Interface
├── Extensions/                  # ENHANCED
│   ├── SkiaSharpExtensions.cs          # ENHANCED - rendering utilities from DrawingThumbnailHandler
│   └── PreferencesExtensions.cs        # Existing - unchanged
├── Constants/                   # UNCHANGED
├── Messages/                    # UNCHANGED
├── Models/                      # UNCHANGED
├── Tools/                       # UNCHANGED
└── ViewModels/                  # UNCHANGED (namespace imports updated)
```

**Structure Decision**: Single project structure is appropriate for this MAUI application. The refactoring creates domain folders within the existing Logic/ folder. Each domain folder represents a cohesive area of functionality (Storage, Input, Canvas, History, Playback, Selection, Layers). This aligns with Constitution VI (modularity by domain) and SOLID principles (high cohesion, low coupling).

## Complexity Tracking

> **No violations requiring justification**

All Constitution gates passed. This refactoring reduces complexity by:
- Eliminating generic folders (Handlers, Utils)
- Decomposing large classes into focused units
- Improving code organization through domain-based structure

## Phase 0: Outline & Research

**Objective**: Resolve domain boundaries, file decomposition strategy, and extension method candidates.

### Research Tasks

1. **Domain Boundary Mapping**
   - **Question**: What are the precise domain boundaries for each file in Handlers/Utils?
   - **Research**: Analyze each file's responsibilities and map to domains (Storage, Input, Canvas, History, Playback, Selection, Layers)
   - **Output**: Domain mapping table in research.md

2. **Large Class Decomposition Strategy**
   - **Question**: How should DrawingStorageMomento (541 lines) and DrawingThumbnailHandler (485 lines) be decomposed?
   - **Research**: Analyze method responsibilities, identify natural boundaries for single-responsibility units
   - **Output**: Decomposition plan with file names and responsibilities

3. **Extension Method Candidates**
   - **Question**: Which methods in DrawingThumbnailHandler and other handlers are candidates for extension methods?
   - **Research**: Identify stateless, reusable methods operating on specific types (e.g., SKPath, SKBitmap)
   - **Output**: Extension method conversion list

4. **Namespace Strategy**
   - **Question**: Should namespaces directly mirror folder structure?
   - **Best Practice**: C# convention is to align namespaces with folder paths
   - **Decision**: `LunaDraw.Logic.Storage`, `LunaDraw.Logic.Input`, etc.
   - **Output**: Namespace mapping table

5. **Dependency Injection Impact**
   - **Question**: Which DI registrations in MauiProgram.cs need updating?
   - **Research**: Identify all Handler/Utils classes registered in DI, determine new interface/class names
   - **Output**: DI update checklist

### Research Deliverable

`research.md` containing:
- Domain boundary decisions with rationale
- File-by-file decomposition plans for large classes
- Extension method conversion candidates
- Namespace mapping strategy
- DI update checklist
- Migration risk assessment

## Phase 1: Design & Contracts

**Prerequisites**: research.md complete with all domain boundaries and decomposition decisions finalized.

### Phase 1.1: Data Model

**Objective**: Document the refactored file structure and class relationships.

**Deliverable**: `data-model.md` containing:

1. **Domain Folder Structure**
   - Each domain folder with its purpose
   - Files within each domain with responsibilities
   - Line count estimates for new files (from decomposition)

2. **Class Relationship Map**
   - Which classes depend on which interfaces
   - Cross-domain dependencies (e.g., Storage → Layers)
   - DI registration changes

3. **File Rename/Move Tracking**
   - Old path → New path mapping for all files
   - Files to be decomposed → new file names
   - Files to be deleted (after decomposition)

4. **Namespace Migration Map**
   - Old namespace → New namespace for all classes
   - Import statement update locations (ViewModels, Pages, etc.)

### Phase 1.2: Contracts

**Objective**: Define migration contracts and validation criteria.

**Deliverable**: `contracts/` directory containing:

1. **`contracts/refactoring-contract.md`**
   - Pre-refactoring checklist (backup, test baseline)
   - Refactoring validation criteria (all tests pass, no files in Handlers/Utils)
   - Post-refactoring verification steps

2. **`contracts/file-migration-manifest.json`**
   - Machine-readable mapping of old → new file paths
   - Decomposition targets with new file names
   - Used by migration scripts (if automated)

3. **`contracts/test-compatibility.md`**
   - Test namespace update strategy
   - Mock object updates for renamed interfaces
   - Test coverage preservation requirements

### Phase 1.3: Quickstart Guide

**Objective**: Provide developers with a clear migration guide.

**Deliverable**: `quickstart.md` containing:

1. **Overview**: What changed and why
2. **Finding Code**: New domain folder structure with examples
3. **Namespace Changes**: How to update import statements
4. **Breaking Changes**: None expected (internal refactoring only)
5. **Migration Checklist**: Steps to sync local branches with refactored code
6. **FAQ**: Common questions (e.g., "Where did DrawingStorageMomento go?")

### Phase 1.4: Agent Context Update

**Action**: Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude`

**Purpose**: Update AI agent context with new folder structure and domain organization for future assistance.

**Result**: `.clinerules/` or `.cursorrules` updated with:
- New domain folder structure
- Naming conventions for domain-based files
- Refactoring principles applied in this feature

## Phase 2: Task Breakdown (To be completed by /speckit.tasks)

**Note**: Phase 2 is NOT executed by `/speckit.plan`. It will be handled by `/speckit.tasks` command.

The `/speckit.tasks` command will generate `tasks.md` with step-by-step implementation tasks, including:
- Pre-refactoring backup and test baseline
- Domain folder creation
- File relocations with `git mv`
- Large class decomposition (DrawingStorageMomento, DrawingThumbnailHandler)
- Extension method extraction
- Namespace updates across codebase
- DI registration updates in MauiProgram.cs
- Test verification
- Cleanup (delete old Handlers/Utils folders)

## Success Metrics (from Spec)

- **SC-001**: Zero files remain in Logic/Handlers and Logic/Utils folders after refactoring
- **SC-002**: All classes exceeding 400 lines are reduced or documented
- **SC-003**: Code location speed improved by 40% (developer survey)
- **SC-004**: 100% of existing unit tests pass without logic changes
- **SC-005**: Positive code review feedback on organization
- **SC-006**: Each domain folder has cohesive, single-sentence describable purpose
- **SC-007**: File names clearly indicate purpose (developer comprehension survey)

## Next Steps

1. **Execute Phase 0**: Generate `research.md` by researching domain boundaries and decomposition strategy
2. **Execute Phase 1**: Generate `data-model.md`, `contracts/`, and `quickstart.md`
3. **Update Agent Context**: Run update script to sync AI agent knowledge
4. **Re-evaluate Constitution Check**: Verify no new violations introduced by design decisions
5. **Proceed to `/speckit.tasks`**: Generate detailed task breakdown for implementation

---

**Plan Status**: ✅ Ready for Phase 0 execution
