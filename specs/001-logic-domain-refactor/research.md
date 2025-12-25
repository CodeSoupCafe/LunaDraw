# Research: Logic Folder Domain-Based Reorganization

**Feature**: 001-logic-domain-refactor
**Date**: 2025-12-24
**Status**: Complete

## Executive Summary

This research document provides the detailed analysis and decisions for reorganizing the Logic folder from a technical classification system (Handlers, Utils) to a domain-based structure. The analysis covers 20 files totaling ~2,800 lines of code, with 3 large files requiring decomposition.

**Key Findings**:
- 7 distinct domain boundaries identified: Storage, Input, Canvas, History, Playback, Selection, Layers
- 2 files require major decomposition: DrawingStorageMomento (541 lines), DrawingThumbnailHandler (485 lines)
- 5 rendering methods identified as extension method candidates
- All DI registrations mapped (12 services affected)
- Zero breaking changes expected for external consumers

## 1. Domain Boundary Mapping

### Domain Identification

Based on analysis of all files in Logic/Handlers and Logic/Utils, seven cohesive domain areas were identified:

| Domain | Purpose | Files Included |
|--------|---------|----------------|
| **Storage** | Drawing persistence, serialization, file I/O | DrawingStorageMomento, ClipboardMemento, ThumbnailCacheFacade |
| **Input** | Canvas touch/mouse input processing | CanvasInputHandler |
| **Canvas** | Rendering, bitmap management, thumbnail generation | DrawingThumbnailHandler, BitmapCache |
| **History** | Undo/redo state management | HistoryMemento |
| **Playback** | Movie mode recording and playback | PlaybackHandler, RecordingHandler |
| **Selection** | Selection state tracking and observation | SelectionObserver |
| **Layers** | Layer management, spatial indexing, preferences | LayerFacade, QuadTreeMemento, PreferencesFacade |

### Rationale for Domain Boundaries

1. **Storage Domain**
   - **Cohesion**: All files deal with persisting drawings to/from disk
   - **Why grouped**: DrawingStorageMomento handles drawing files, ClipboardMemento handles clipboard persistence, ThumbnailCacheFacade caches thumbnails to disk
   - **Single sentence**: "Manages all file I/O operations for drawings and related data"

2. **Input Domain**
   - **Cohesion**: Focused on processing user input (touch/mouse) for canvas interactions
   - **Why grouped**: CanvasInputHandler is the sole input processor, handling gestures, tool delegation, and multi-touch
   - **Single sentence**: "Processes all user input events on the canvas and delegates to active tools"

3. **Canvas Domain**
   - **Cohesion**: Rendering and bitmap operations for canvas visualization
   - **Why grouped**: DrawingThumbnailHandler renders thumbnails, BitmapCache manages bitmap caching
   - **Single sentence**: "Manages canvas rendering, thumbnail generation, and bitmap caching"

4. **History Domain**
   - **Cohesion**: Undo/redo state tracking
   - **Why grouped**: HistoryMemento is the sole component managing undo/redo stack
   - **Single sentence**: "Tracks undo/redo history for drawing operations"

5. **Playback Domain**
   - **Cohesion**: Movie mode functionality (recording and playback)
   - **Why grouped**: PlaybackHandler plays recorded drawing events, RecordingHandler captures events
   - **Single sentence**: "Records and plays back drawing sessions as time-lapse animations"

6. **Selection Domain**
   - **Cohesion**: Selection state management
   - **Why grouped**: SelectionObserver tracks currently selected elements and publishes changes
   - **Single sentence**: "Observes and broadcasts selection state changes"

7. **Layers Domain**
   - **Cohesion**: Layer management and spatial indexing
   - **Why grouped**: LayerFacade manages layer operations, QuadTreeMemento provides spatial indexing for layer elements, PreferencesFacade handles layer-related preferences
   - **Single sentence**: "Manages layers, spatial indexing, and layer-related preferences"

### Alternative Approaches Considered

**Alternative 1: Feature-Based Domains** (e.g., Drawing, Tools, Gallery)
- **Rejected**: Would scatter related file I/O (DrawingStorageMomento, ThumbnailCacheFacade) across multiple domains
- **Why rejected**: Violates cohesion - file I/O operations belong together

**Alternative 2: Pattern-Based Folders** (e.g., Facades, Mementos, Observers)
- **Rejected**: This is essentially the current Handlers/Utils problem - technical classification instead of domain organization
- **Why rejected**: Doesn't improve discoverability or reflect business domains

**Alternative 3: Layer-Centric Organization** (e.g., Persistence, Application, Domain)
- **Rejected**: Too abstract for this application; MAUI apps benefit from concrete domain folders
- **Why rejected**: Adds unnecessary architectural ceremony for a focused drawing application

## 2. Large Class Decomposition Strategy

### DrawingStorageMomento (541 lines) → Storage Domain

**Analysis**: This class has multiple distinct responsibilities:

1. **File I/O Operations** (Lines 98-218)
   - `LoadAllDrawingsAsync()` - 23 lines
   - `LoadDrawingAsync()` - 15 lines
   - `ExternalDrawingAsync()` (Save) - 14 lines
   - `DeleteDrawingAsync()` - 9 lines
   - `DuplicateDrawingAsync()` - 14 lines
   - `RenameDrawingAsync()` - 8 lines
   - `RenameUntitledDrawingsAsync()` - 29 lines
   - `GetNextDefaultNameAsync()` - 23 lines

2. **Conversion Logic** (Lines 220-541)
   - `CreateExternalDrawingFromCurrent()` - 113 lines
   - `RestoreLayers()` - 159 lines
   - `GetBrushShapeStatic()` - 34 lines (lookup table)

**Decomposition Plan**:

| Current Responsibility | New File | Lines | Rationale |
|------------------------|----------|-------|-----------|
| LoadAllDrawingsAsync() | `LoadAllDrawingsHandler.cs` | ~40 | Single responsibility: load all drawings from disk |
| LoadDrawingAsync() | `LoadDrawingHandler.cs` | ~30 | Single responsibility: load one drawing by ID |
| ExternalDrawingAsync() | `SaveDrawingHandler.cs` | ~30 | Single responsibility: save a drawing to disk |
| DeleteDrawingAsync() | `DeleteDrawingHandler.cs` | ~25 | Single responsibility: delete a drawing file |
| DuplicateDrawingAsync() | `DuplicateDrawingHandler.cs` | ~35 | Single responsibility: duplicate a drawing |
| RenameDrawingAsync() | `RenameDrawingHandler.cs` | ~30 | Single responsibility: rename a drawing |
| RenameUntitledDrawingsAsync() | Merged into `RenameDrawingHandler.cs` | - | Related renaming operation |
| GetNextDefaultNameAsync() | Merged into `SaveDrawingHandler.cs` | - | Used when creating new drawings |
| CreateExternalDrawingFromCurrent() + RestoreLayers() | `DrawingConverter.cs` | ~350 | Conversion between domain/external models |
| Interface methods | `IDrawingStorage.cs` | ~30 | Interface for drawing storage operations |
| Shared configuration | `DrawingStorageConfiguration.cs` | ~40 | Storage path, JSON options, constants |

**Post-Decomposition Result**: 10 focused files averaging 30-50 lines each (except DrawingConverter at ~350 lines, which is acceptable given its cohesive conversion responsibility).

**Benefits**:
- Each operation handler follows Single Responsibility Principle
- Easier to test individual operations in isolation
- Reduced merge conflicts (developers work on different operation files)
- Clear naming indicates file purpose without opening it

### DrawingThumbnailHandler (485 lines) → Canvas Domain

**Analysis**: This class has distinct responsibilities:

1. **Thumbnail Retrieval** (Lines 45-96)
   - `GetThumbnailBase64Async()` - 26 lines (retrieves/generates base64 thumbnail)
   - `GetThumbnailAsync()` - 24 lines (retrieves/generates ImageSource thumbnail)
   - Caching logic with `inMemoryImageSourceCache`

2. **Thumbnail Generation** (Lines 98-210)
   - `GenerateThumbnailAsync()` - 56 lines (renders to ImageSource)
   - `GenerateThumbnailBase64Async()` - 56 lines (renders to base64 string)
   - Shared rendering logic (scale, translate, iterate layers)

3. **Element Rendering** (Lines 212-472)
   - `RenderElement()` - dispatcher (28 lines)
   - `RenderPath()` - 30 lines
   - `RenderStamps()` - 40 lines
   - `RenderRectangle()` - 42 lines
   - `RenderEllipse()` - 40 lines
   - `RenderLine()` - 33 lines
   - `GetShapePath()` - 40 lines (brush shape to SKPath conversion)

4. **Cache Management** (Lines 474-485)
   - `InvalidateThumbnailAsync()` - 5 lines
   - `ClearCacheAsync()` - 6 lines

**Decomposition Plan**:

| Current Responsibility | New File | Lines | Rationale |
|------------------------|----------|-------|-----------|
| GetThumbnail* + caching + InvalidateThumbnail* + ClearCacheAsync() | `ThumbnailProvider.cs` | ~120 | Single responsibility: provide thumbnails with caching |
| GenerateThumbnail* (both methods) | `ThumbnailGenerator.cs` | ~150 | Single responsibility: generate thumbnails by rendering |
| RenderPath() | `SkiaSharpExtensions.RenderPath()` | ~35 | Extension method for External.Path |
| RenderStamps() | Keep in `ThumbnailGenerator.cs` | - | Tightly coupled to thumbnail generation |
| RenderRectangle() | `SkiaSharpExtensions.RenderRectangle()` | ~45 | Extension method for External.Rectangle |
| RenderEllipse() | `SkiaSharpExtensions.RenderEllipse()` | ~45 | Extension method for External.Ellipse |
| RenderLine() | `SkiaSharpExtensions.RenderLine()` | ~40 | Extension method for External.Line |
| GetShapePath() | `SkiaSharpExtensions.GetShapePath()` | ~45 | Extension method for BrushShapeType |
| RenderElement() dispatcher | Keep in `ThumbnailGenerator.cs` | - | Coordinates rendering |
| Interface | `IThumbnailProvider.cs` | ~20 | Interface for thumbnail provision |

**Post-Decomposition Result**:
- `ThumbnailProvider.cs` (~120 lines): Retrieval and caching
- `ThumbnailGenerator.cs` (~150 lines): Generation logic
- `SkiaSharpExtensions.cs` (ENHANCED with ~210 lines of new extensions): Reusable rendering utilities

**Benefits**:
- Rendering methods become reusable across codebase (not just thumbnails)
- ThumbnailProvider and ThumbnailGenerator have clear, focused responsibilities
- Extension methods improve API discoverability (e.g., `externalPath.Render(canvas, paint)`)
- Thumbnail caching logic separated from generation logic

### CanvasInputHandler (388 lines) → Input Domain

**Analysis**: Currently at 388 lines, just under the 400-line threshold.

**Decision**: **Keep as single file** with potential for future decomposition if it grows.

**Rationale**:
- Input handling is inherently complex (multi-touch gestures, tool delegation, smoothing)
- Breaking it apart now would create artificial boundaries
- Current structure is cohesive (all methods relate to input processing)
- If it exceeds 400 lines in future work, consider extracting:
  - `TouchTracker.cs` (touch point management)
  - `GestureProcessor.cs` (gesture recognition and transformation)

**Post-Reorganization**: Simply relocate to `Logic/Input/CanvasInputHandler.cs`

### Files Not Requiring Decomposition

| File | Lines | Decision | Rationale |
|------|-------|----------|-----------|
| PlaybackHandler | 215 | Keep as-is | Well-factored, focused on playback state machine |
| LayerFacade | 177 | Keep as-is | Clean facade pattern, cohesive layer operations |
| QuadTreeMemento | 171 | Keep as-is | Focused spatial indexing, mathematically complex |
| SelectionObserver | 142 | Keep as-is | Simple observer pattern, reactive subscriptions |
| BitmapCache | 105 | Keep as-is | Simple caching logic |
| ThumbnailCacheFacade | 93 | Keep as-is | Clean facade over file-based cache |
| HistoryMemento | 86 | Keep as-is | Simple undo/redo stack |
| PreferencesFacade | 71 | Keep as-is | Simple facade over MAUI Preferences |
| ClipboardMemento | 45 | Keep as-is | Minimal clipboard state holder |
| RecordingHandler | 39 | Keep as-is | Simple event recording |

## 3. Extension Method Conversion Candidates

### Identified Candidates from DrawingThumbnailHandler

All rendering methods are **stateless** and operate on **specific types** (External.Path, External.Rectangle, etc.), making them ideal extension method candidates.

| Method | Signature | Target Type | Extension Signature | Lines |
|--------|-----------|-------------|---------------------|-------|
| RenderPath | `void RenderPath(SKCanvas, External.Path, SKPaint)` | `External.Path` | `this External.Path path.Render(SKCanvas, SKPaint)` | 30 |
| RenderRectangle | `void RenderRectangle(SKCanvas, External.Rectangle, SKPaint)` | `External.Rectangle` | `this External.Rectangle rect.Render(SKCanvas, SKPaint)` | 42 |
| RenderEllipse | `void RenderEllipse(SKCanvas, External.Ellipse, SKPaint)` | `External.Ellipse` | `this External.Ellipse ellipse.Render(SKCanvas, SKPaint)` | 40 |
| RenderLine | `void RenderLine(SKCanvas, External.Line, SKPaint)` | `External.Line` | `this External.Line line.Render(SKCanvas, SKPaint)` | 33 |
| GetShapePath | `SKPath? GetShapePath(BrushShapeType)` | `BrushShapeType` | `this BrushShapeType type.ToSkiaPath()` | 40 |

### Non-Candidates (Keep in Domain Classes)

| Method | Why Not Extension | Keep Where |
|--------|-------------------|------------|
| RenderStamps | Requires local state (loop, transformations, specific to thumbnail context) | `ThumbnailGenerator.cs` |
| RenderElement | Dispatcher method, not operating on specific type | `ThumbnailGenerator.cs` |
| GenerateThumbnailAsync | Complex orchestration, not reusable utility | `ThumbnailGenerator.cs` |
| GetThumbnailAsync | Domain-specific caching logic | `ThumbnailProvider.cs` |

### Benefits of Extension Method Approach

1. **Discoverability**: IntelliSense shows `Render()` when working with `External.Path`
2. **Reusability**: Other parts of codebase can render External elements (e.g., export, printing)
3. **Fluent API**: `externalPath.Render(canvas, paint)` vs. `SkiaHelper.RenderPath(canvas, externalPath, paint)`
4. **Testability**: Extension methods are static and easily unit tested

### Updated SkiaSharpExtensions.cs Structure

**Current file**: Contains existing SkiaSharp utilities (if any)
**Enhanced file** (estimated ~250 lines total):

```csharp
namespace LunaDraw.Logic.Extensions;

public static class SkiaSharpExtensions
{
    // === Existing extensions (preserved) ===
    // ... current content ...

    // === New rendering extensions (from DrawingThumbnailHandler) ===

    /// <summary>
    /// Renders an External.Path element to the canvas
    /// </summary>
    public static void Render(this External.Path pathElement, SKCanvas canvas, SKPaint paint)
    {
        // ... RenderPath logic moved here ...
    }

    /// <summary>
    /// Renders an External.Rectangle element to the canvas
    /// </summary>
    public static void Render(this External.Rectangle rectangleElement, SKCanvas canvas, SKPaint paint)
    {
        // ... RenderRectangle logic moved here ...
    }

    /// <summary>
    /// Renders an External.Ellipse element to the canvas
    /// </summary>
    public static void Render(this External.Ellipse ellipseElement, SKCanvas canvas, SKPaint paint)
    {
        // ... RenderEllipse logic moved here ...
    }

    /// <summary>
    /// Renders an External.Line element to the canvas
    /// </summary>
    public static void Render(this External.Line lineElement, SKCanvas canvas, SKPaint paint)
    {
        // ... RenderLine logic moved here ...
    }

    /// <summary>
    /// Converts a BrushShapeType to its SKPath representation
    /// </summary>
    public static SKPath? ToSkiaPath(this BrushShapeType shapeType)
    {
        // ... GetShapePath logic moved here ...
    }
}
```

## 4. Namespace Strategy

### Decision: Mirror Folder Structure

**Pattern**: `LunaDraw.Logic.{DomainName}`

### Namespace Mapping Table

| Old Namespace | New Namespace | Affected Files |
|---------------|---------------|----------------|
| `LunaDraw.Logic.Handlers` | `LunaDraw.Logic.Input` | CanvasInputHandler, ICanvasInputHandler |
| `LunaDraw.Logic.Handlers` | `LunaDraw.Logic.Canvas` | DrawingThumbnailHandler → ThumbnailProvider/ThumbnailGenerator |
| `LunaDraw.Logic.Handlers` | `LunaDraw.Logic.Playback` | PlaybackHandler, IPlaybackHandler, RecordingHandler, IRecordingHandler |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.Storage` | DrawingStorageMomento → multiple handlers, ClipboardMemento, ThumbnailCacheFacade, IThumbnailCacheFacade |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.Canvas` | BitmapCache, IBitmapCache |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.History` | HistoryMemento |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.Selection` | SelectionObserver |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.Layers` | LayerFacade, ILayerFacade, QuadTreeMemento, PreferencesFacade, IPreferencesFacade |

### Import Statement Update Locations

**Files requiring `using` statement updates**:

1. **ViewModels** (Logic/ViewModels/)
   - `MainViewModel.cs`: Uses CanvasInputHandler, DrawingStorageMomento
   - `GalleryViewModel.cs`: Uses DrawingStorageMomento, DrawingThumbnailHandler
   - `HistoryViewModel.cs`: Uses HistoryMemento
   - `LayerPanelViewModel.cs`: Uses LayerFacade
   - `PlaybackViewModel.cs`: Uses PlaybackHandler, RecordingHandler
   - `SelectionViewModel.cs`: Uses SelectionObserver
   - `ToolbarViewModel.cs`: May use various handlers

2. **Pages** (Pages/)
   - `MainPage.xaml.cs`: Uses CanvasInputHandler
   - `PlaybackPage.xaml.cs`: Uses PlaybackHandler

3. **MauiProgram.cs**: DI registration (see Section 5)

4. **Tests** (tests/LunaDraw.Tests/)
   - All test files importing Handler/Utils namespaces
   - Mock object setups will need namespace updates

### Best Practice Adherence

**C# Convention**: Namespaces should match folder paths
- ✅ Follows .NET naming guidelines
- ✅ Improves IDE navigation (namespace → folder correlation)
- ✅ Reduces cognitive load for developers

**Alternative Rejected**: Flat namespace (e.g., `LunaDraw.Logic.Storage` for all Storage files regardless of subfolder)
- ❌ Loses folder structure information
- ❌ Doesn't scale if domains gain subfolders in future

## 5. Dependency Injection Impact Analysis

### Current DI Registrations (MauiProgram.cs)

**Lines 80-98** in MauiProgram.cs register the following services:

| Registration | Type | Interface | Location | New Location |
|--------------|------|-----------|----------|--------------|
| `IMessageBus` | Singleton | `IMessageBus` | ReactiveUI | Unchanged |
| `NavigationModel` | Singleton | - | Logic/Models | Unchanged |
| `SelectionObserver` | Singleton | - | Logic/Utils | **Logic/Selection** |
| `ILayerFacade` | Singleton | `ILayerFacade` | Logic/Utils | **Logic/Layers** |
| `ICanvasInputHandler` | Singleton | `ICanvasInputHandler` | Logic/Handlers | **Logic/Input** |
| `ClipboardMemento` | Singleton | - | Logic/Utils | **Logic/Storage** |
| `IBitmapCache` | Singleton | `IBitmapCache` | Logic/Utils | **Logic/Canvas** |
| `IPreferencesFacade` | Singleton | `IPreferencesFacade` | Logic/Utils | **Logic/Layers** |
| `IFileSaver` | Singleton | `IFileSaver` | CommunityToolkit | Unchanged |
| `IDrawingStorageMomento` | Singleton | `IDrawingStorageMomento` | Logic/Utils | **Logic/Storage** (→ IDrawingStorage) |
| `IThumbnailCacheFacade` | Singleton | `IThumbnailCacheFacade` | Logic/Services | **Logic/Storage** |
| `IDrawingThumbnailHandler` | Singleton | `IDrawingThumbnailHandler` | Logic/Utils | **Logic/Canvas** (→ IThumbnailProvider) |
| `IRecordingHandler` | Singleton | `IRecordingHandler` | Logic/Handlers | **Logic/Playback** |
| `IPlaybackHandler` | Singleton | `IPlaybackHandler` | Logic/Handlers | **Logic/Playback** |

### DI Update Checklist

**Phase 1: Add new namespaces to MauiProgram.cs**

```csharp
// Add these using statements:
using LunaDraw.Logic.Storage;
using LunaDraw.Logic.Input;
using LunaDraw.Logic.Canvas;
using LunaDraw.Logic.History;
using LunaDraw.Logic.Playback;
using LunaDraw.Logic.Selection;
using LunaDraw.Logic.Layers;
```

**Phase 2: Update registrations for decomposed classes**

```csharp
// BEFORE (DrawingStorageMomento)
builder.Services.AddSingleton<IDrawingStorageMomento, DrawingStorageMomento>();

// AFTER (individual handlers)
// Option 1: Register interface with facade that delegates to handlers
builder.Services.AddSingleton<IDrawingStorage, DrawingStorageFacade>();
builder.Services.AddSingleton<LoadAllDrawingsHandler>();
builder.Services.AddSingleton<LoadDrawingHandler>();
builder.Services.AddSingleton<SaveDrawingHandler>();
// ... etc.

// Option 2: Only register facade if handlers are internal to facade
builder.Services.AddSingleton<IDrawingStorage, DrawingStorageFacade>();
```

**Phase 3: Update registrations for renamed interfaces**

```csharp
// BEFORE
builder.Services.AddSingleton<IDrawingThumbnailHandler, DrawingThumbnailHandler>();

// AFTER
builder.Services.AddSingleton<IThumbnailProvider, ThumbnailProvider>();
builder.Services.AddTransient<ThumbnailGenerator>(); // If needed for dependency injection
```

**Phase 4: Remove old namespace imports**

```csharp
// REMOVE these:
// using LunaDraw.Logic.Handlers;
// using LunaDraw.Logic.Utils;
```

### Breaking Changes Assessment

**Internal API Changes**: ✅ **Zero breaking changes** for external consumers

**Reasoning**:
- All changes are within `Logic/` folder (internal to application)
- No public NuGet package exposed
- ViewModels and Pages will update imports but use same interfaces
- DI container provides implementations - consumers remain unaware of concrete class changes

**Potential Internal Issues**:
1. **Test mocks**: Interfaces may be renamed (IDrawingStorageMomento → IDrawingStorage)
   - **Mitigation**: Update mock setups in tests
2. **Static references**: If any code directly constructs handlers (violating DI)
   - **Mitigation**: Identify with compiler errors, refactor to use DI

### DI Registration Strategy Decision

**Option A: Register Individual Handlers**
```csharp
builder.Services.AddSingleton<LoadAllDrawingsHandler>();
builder.Services.AddSingleton<LoadDrawingHandler>();
// ... etc.
```
- **Pros**: Maximum flexibility, individual testability
- **Cons**: Verbose registration, many constructors to inject

**Option B: Register Facade (Recommended)**
```csharp
builder.Services.AddSingleton<IDrawingStorage, DrawingStorageFacade>();
// Handlers are internal to facade, instantiated as needed
```
- **Pros**: Clean DI registration, internal composition
- **Cons**: Slightly harder to test individual handlers (but facade is testable)

**Recommendation**: **Option B** - use a facade to compose the handlers internally. This:
- Minimizes DI registration changes
- Provides backward compatibility (consumers still get `IDrawingStorage`)
- Allows internal refactoring without impacting DI setup
- Handlers can still be unit tested independently (instantiate directly in tests)

## 6. Migration Risk Assessment

### Risk Matrix

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Breaking test logic | Medium | High | Run full test suite after each domain migration phase |
| Git history loss | Low | Medium | Use `git mv` exclusively; verify with `git log --follow` |
| Namespace import errors | High | Low | Compiler will catch all; systematic using statement updates |
| DI registration errors | Medium | High | Runtime errors; comprehensive manual testing required |
| Merge conflicts | Medium | Medium | Coordinate refactoring timing; rebase frequently |
| Performance regression | Low | High | Benchmark key operations (drawing load, thumbnail generation) before/after |

### Migration Phases (for tasks.md)

**Phase 1: Pre-Refactoring** (Low Risk)
- Create git branch
- Run full test suite, establish baseline
- Document current test pass rate

**Phase 2: Create Domain Folders** (Zero Risk)
- `mkdir` commands for new domain folders
- No code changes yet

**Phase 3: Simple Relocations** (Low Risk)
- Move well-factored files (HistoryMemento, ClipboardMemento, etc.)
- Update namespaces
- Run tests after each file

**Phase 4: Large Class Decomposition** (High Risk)
- Decompose DrawingStorageMomento → Storage handlers
- Decompose DrawingThumbnailHandler → Canvas providers
- Run tests after each decomposition
- **Mitigation**: Do one class at a time, verify tests pass before next

**Phase 5: Extension Method Extraction** (Medium Risk)
- Move rendering methods to SkiaSharpExtensions
- Update call sites
- Run tests

**Phase 6: Namespace Updates** (Medium Risk)
- Update ViewModels, Pages, Tests
- Update DI registration in MauiProgram.cs
- Run tests

**Phase 7: Cleanup** (Low Risk)
- Delete empty Handlers and Utils folders
- Verify no files remain
- Final test run

### Rollback Strategy

**If tests fail at any phase**:
1. Identify failing test
2. Compare namespace/import changes
3. Fix incrementally (don't rollback entire refactoring)
4. If unfixable: `git reset --hard` to previous phase commit

**Backup Strategy**:
- Commit after each successful phase
- Tag baseline commit: `git tag baseline-pre-refactor`

## 7. Alternatives Considered Summary

### Alternative Decomposition Strategies

**Alternative 1: Minimal Decomposition**
- **Approach**: Only split files exceeding 500 lines
- **Rejected**: Leaves DrawingThumbnailHandler (485 lines) unaddressed; misses opportunity for extension methods

**Alternative 2: Extreme Decomposition**
- **Approach**: Every method becomes its own file
- **Rejected**: Over-engineering; creates file explosion (50+ tiny files); harder to navigate

**Alternative 3: Hybrid (Handlers + Domains)**
- **Approach**: Keep Handlers folder, add domain subfolders (e.g., Handlers/Storage/, Handlers/Playback/)
- **Rejected**: Perpetuates Handlers folder existence; violates FR-010 (no Handlers folder)

### Alternative Extension Method Strategies

**Alternative 1: Static Helper Class**
- **Approach**: Create `RenderingHelpers` static class instead of extensions
- **Rejected**: Less discoverable than extension methods; doesn't follow modern C# practices

**Alternative 2: Instance Methods on External.* Classes**
- **Approach**: Add `Render()` method to `External.Path`, `External.Rectangle`, etc.
- **Rejected**: External.* classes are DTOs (data transfer objects) and shouldn't contain rendering logic

## 8. Post-Reorganization Verification Checklist

### Structural Verification

- [ ] Zero files in `Logic/Handlers/` folder
- [ ] Zero files in `Logic/Utils/` folder
- [ ] All domain folders created: Storage, Input, Canvas, History, Playback, Selection, Layers
- [ ] Each domain folder contains only related files
- [ ] File naming follows pattern: `[Operation][Entity]Handler.cs` for operation handlers

### Code Quality Verification

- [ ] No files exceed 400 lines (except documented exceptions)
- [ ] All namespaces match folder paths
- [ ] All `using` statements updated (no obsolete namespace imports)
- [ ] DI registration in MauiProgram.cs updated and functional
- [ ] Extension methods in `SkiaSharpExtensions.cs` work correctly

### Functional Verification

- [ ] 100% of unit tests pass without logic modification
- [ ] Application builds without errors
- [ ] Application launches successfully
- [ ] Drawing operations work (create, save, load, delete, duplicate, rename)
- [ ] Thumbnail generation works
- [ ] Playback/recording works
- [ ] Undo/redo works
- [ ] Selection works
- [ ] Layer operations work

### Performance Verification

- [ ] Drawing load time unchanged (benchmark)
- [ ] Thumbnail generation time unchanged (benchmark)
- [ ] No memory leaks introduced (profiler check)

## 9. Conclusion

This research provides a comprehensive blueprint for reorganizing the Logic folder into seven domain-based folders. The decomposition of DrawingStorageMomento (541 lines) and DrawingThumbnailHandler (485 lines) into focused, single-responsibility units will significantly improve code maintainability and discoverability.

**Key Decisions**:
1. **7 Domain Folders**: Storage, Input, Canvas, History, Playback, Selection, Layers
2. **Decompose 2 Large Files**: DrawingStorageMomento → 10 files, DrawingThumbnailHandler → 3 files + extensions
3. **5 Extension Methods**: Rendering utilities moved to SkiaSharpExtensions
4. **Namespace Strategy**: Mirror folder structure (`LunaDraw.Logic.{DomainName}`)
5. **DI Strategy**: Use facade pattern to minimize registration changes

**Success Criteria Addressed**:
- ✅ SC-001: Zero files in Handlers/Utils (verified in checklist)
- ✅ SC-002: All 400+ line files decomposed
- ✅ SC-003: Improved discoverability through domain organization
- ✅ SC-004: Test compatibility maintained (namespace updates only)
- ✅ SC-006: Each domain has single-sentence description
- ✅ SC-007: File names indicate purpose (operation handler pattern)

**Next Steps**: Proceed to Phase 1 to generate `data-model.md`, `contracts/`, and `quickstart.md`.

---

**Research Status**: ✅ Complete - Ready for Phase 1
