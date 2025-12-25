# Data Model: Logic Folder Refactored Structure

**Feature**: 001-logic-domain-refactor
**Date**: 2025-12-24
**Status**: Complete

## Overview

This document maps the complete refactored structure of the Logic folder, including all file relocations, decompositions, and new files created through the reorganization process.

## Domain Folder Structure

### Storage Domain (`Logic/Storage/`)

**Purpose**: Manages all file I/O operations for drawings and related data persistence.

| File | Lines (est.) | Responsibility | Decomposed From |
|------|--------------|----------------|-----------------|
| `IDrawingStorage.cs` | 30 | Interface for drawing storage operations | DrawingStorageMomento |
| `DrawingStorageConfiguration.cs` | 40 | Storage path, JSON serialization options, file lock | DrawingStorageMomento |
| `LoadAllDrawingsHandler.cs` | 40 | Loads all drawings from disk with sorting | DrawingStorageMomento.LoadAllDrawingsAsync() |
| `LoadDrawingHandler.cs` | 30 | Loads a single drawing by ID | DrawingStorageMomento.LoadDrawingAsync() |
| `SaveDrawingHandler.cs` | 45 | Saves a drawing to disk (includes GetNextDefaultNameAsync logic) | DrawingStorageMomento.ExternalDrawingAsync() |
| `DeleteDrawingHandler.cs` | 25 | Deletes a drawing file | DrawingStorageMomento.DeleteDrawingAsync() |
| `DuplicateDrawingHandler.cs` | 35 | Duplicates an existing drawing | DrawingStorageMomento.DuplicateDrawingAsync() |
| `RenameDrawingHandler.cs` | 50 | Renames one or batch renames untitled drawings | DrawingStorageMomento.RenameDrawingAsync() + RenameUntitledDrawingsAsync() |
| `DrawingConverter.cs` | 350 | Converts between domain models (Layer) and external models (External.Drawing) | DrawingStorageMomento.CreateExternalDrawingFromCurrent() + RestoreLayers() |
| `ClipboardMemento.cs` | 45 | Clipboard state persistence | Relocated from Utils |
| `ThumbnailCacheFacade.cs` | 93 | Persistent thumbnail cache facade | Relocated from Utils |
| `IThumbnailCacheFacade.cs` | 20 | Interface for thumbnail caching | Relocated from Utils |

**Total**: 12 files, ~753 lines

### Input Domain (`Logic/Input/`)

**Purpose**: Processes all user input events on the canvas and delegates to active tools.

| File | Lines (est.) | Responsibility | Decomposed From |
|------|--------------|----------------|-----------------|
| `CanvasInputHandler.cs` | 388 | Main input handler: touch/mouse, gestures, tool delegation | Relocated from Handlers (as-is) |
| `ICanvasInputHandler.cs` | 34 | Interface for canvas input handling | Relocated from Handlers |

**Total**: 2 files, ~422 lines

**Note**: CanvasInputHandler remains intact at 388 lines (under 400-line threshold). Future growth may warrant extraction of TouchTracker or GestureProcessor.

### Canvas Domain (`Logic/Canvas/`)

**Purpose**: Manages canvas rendering, thumbnail generation, and bitmap caching.

| File | Lines (est.) | Responsibility | Decomposed From |
|------|--------------|----------------|-----------------|
| `IThumbnailProvider.cs` | 25 | Interface for thumbnail provision | DrawingThumbnailHandler (renamed from IDrawingThumbnailHandler) |
| `ThumbnailProvider.cs` | 120 | Retrieves thumbnails with in-memory and persistent caching | DrawingThumbnailHandler.GetThumbnail*() methods |
| `ThumbnailGenerator.cs` | 150 | Generates thumbnails by rendering drawings (includes RenderElement dispatcher, RenderStamps) | DrawingThumbnailHandler.GenerateThumbnail*() methods |
| `BitmapCache.cs` | 105 | Bitmap caching for canvas operations | Relocated from Utils (as-is) |
| `IBitmapCache.cs` | 15 | Interface for bitmap caching | Relocated from Utils |

**Total**: 5 files, ~415 lines

**Extension Methods**: Rendering methods moved to `Logic/Extensions/SkiaSharpExtensions.cs` (see Extensions section).

### History Domain (`Logic/History/`)

**Purpose**: Tracks undo/redo history for drawing operations.

| File | Lines (est.) | Responsibility | Decomposed From |
|------|--------------|----------------|-----------------|
| `HistoryMemento.cs` | 86 | Undo/redo stack management | Relocated from Utils (as-is) |

**Total**: 1 file, ~86 lines

**Note**: Well-factored class; no decomposition needed.

### Playback Domain (`Logic/Playback/`)

**Purpose**: Records and plays back drawing sessions as time-lapse animations (Movie Mode).

| File | Lines (est.) | Responsibility | Decomposed From |
|------|--------------|----------------|-----------------|
| `IPlaybackHandler.cs` | 62 | Interface for playback operations | Relocated from Handlers |
| `PlaybackHandler.cs` | 215 | Playback state machine and event replay | Relocated from Handlers (as-is) |
| `IRecordingHandler.cs` | 38 | Interface for recording operations | Relocated from Handlers |
| `RecordingHandler.cs` | 39 | Records drawing events for playback | Relocated from Handlers (as-is) |

**Total**: 4 files, ~354 lines

### Selection Domain (`Logic/Selection/`)

**Purpose**: Observes and broadcasts selection state changes.

| File | Lines (est.) | Responsibility | Decomposed From |
|------|--------------|----------------|-----------------|
| `SelectionObserver.cs` | 142 | Tracks selected elements and publishes reactive changes | Relocated from Utils (as-is) |

**Total**: 1 file, ~142 lines

**Note**: Clean observer pattern implementation; no decomposition needed.

### Layers Domain (`Logic/Layers/`)

**Purpose**: Manages layers, spatial indexing, and layer-related preferences.

| File | Lines (est.) | Responsibility | Decomposed From |
|------|--------------|----------------|-----------------|
| `ILayerFacade.cs` | 41 | Interface for layer management | Relocated from Utils |
| `LayerFacade.cs` | 177 | Facade for layer operations (add, remove, move elements) | Relocated from Utils (as-is) |
| `QuadTreeMemento.cs` | 171 | Spatial indexing for efficient layer element queries | Relocated from Utils (as-is) |
| `IPreferencesFacade.cs` | 36 | Interface for preferences management | Relocated from Utils |
| `PreferencesFacade.cs` | 71 | Facade over MAUI Preferences for layer settings | Relocated from Utils (as-is) |

**Total**: 5 files, ~496 lines

### Extensions (Enhanced) (`Logic/Extensions/`)

**Purpose**: Reusable extension methods for SkiaSharp and other types.

| File | Lines (est.) | Responsibility | Changes |
|------|--------------|----------------|---------|
| `SkiaSharpExtensions.cs` | ~250 | **ENHANCED**: Existing extensions + new rendering extensions for External.* elements | Added ~210 lines from DrawingThumbnailHandler |
| `PreferencesExtensions.cs` | Unchanged | Existing preferences extensions | No changes |

**New Extension Methods** (in SkiaSharpExtensions.cs):

```csharp
// From DrawingThumbnailHandler.RenderPath (30 lines)
public static void Render(this External.Path pathElement, SKCanvas canvas, SKPaint paint)

// From DrawingThumbnailHandler.RenderRectangle (42 lines)
public static void Render(this External.Rectangle rectangleElement, SKCanvas canvas, SKPaint paint)

// From DrawingThumbnailHandler.RenderEllipse (40 lines)
public static void Render(this External.Ellipse ellipseElement, SKCanvas canvas, SKPaint paint)

// From DrawingThumbnailHandler.RenderLine (33 lines)
public static void Render(this External.Line lineElement, SKCanvas canvas, SKPaint paint)

// From DrawingThumbnailHandler.GetShapePath (40 lines)
public static SKPath? ToSkiaPath(this BrushShapeType shapeType)
```

**Total New Content**: ~185 lines of rendering extensions (with some refactoring for consistency)

## File Migration Tracking

### Relocations (No Decomposition)

| Old Path | New Path | Lines | Change Type |
|----------|----------|-------|-------------|
| `Logic/Utils/ClipboardMemento.cs` | `Logic/Storage/ClipboardMemento.cs` | 45 | Relocate + Namespace update |
| `Logic/Utils/ThumbnailCacheFacade.cs` | `Logic/Storage/ThumbnailCacheFacade.cs` | 93 | Relocate + Namespace update |
| `Logic/Utils/IThumbnailCacheFacade.cs` | `Logic/Storage/IThumbnailCacheFacade.cs` | 20 | Relocate + Namespace update |
| `Logic/Handlers/CanvasInputHandler.cs` | `Logic/Input/CanvasInputHandler.cs` | 388 | Relocate + Namespace update |
| `Logic/Handlers/ICanvasInputHandler.cs` | `Logic/Input/ICanvasInputHandler.cs` | 34 | Relocate + Namespace update |
| `Logic/Utils/BitmapCache.cs` | `Logic/Canvas/BitmapCache.cs` | 105 | Relocate + Namespace update |
| `Logic/Utils/IBitmapCache.cs` | `Logic/Canvas/IBitmapCache.cs` | 15 | Relocate + Namespace update (assumed exists) |
| `Logic/Utils/HistoryMemento.cs` | `Logic/History/HistoryMemento.cs` | 86 | Relocate + Namespace update |
| `Logic/Handlers/PlaybackHandler.cs` | `Logic/Playback/PlaybackHandler.cs` | 215 | Relocate + Namespace update |
| `Logic/Handlers/IPlaybackHandler.cs` | `Logic/Playback/IPlaybackHandler.cs` | 62 | Relocate + Namespace update |
| `Logic/Handlers/RecordingHandler.cs` | `Logic/Playback/RecordingHandler.cs` | 39 | Relocate + Namespace update |
| `Logic/Handlers/IRecordingHandler.cs` | `Logic/Playback/IRecordingHandler.cs` | 38 | Relocate + Namespace update |
| `Logic/Utils/SelectionObserver.cs` | `Logic/Selection/SelectionObserver.cs` | 142 | Relocate + Namespace update |
| `Logic/Utils/LayerFacade.cs` | `Logic/Layers/LayerFacade.cs` | 177 | Relocate + Namespace update |
| `Logic/Utils/ILayerFacade.cs` | `Logic/Layers/ILayerFacade.cs` | 41 | Relocate + Namespace update |
| `Logic/Utils/QuadTreeMemento.cs` | `Logic/Layers/QuadTreeMemento.cs` | 171 | Relocate + Namespace update |
| `Logic/Utils/PreferencesFacade.cs` | `Logic/Layers/PreferencesFacade.cs` | 71 | Relocate + Namespace update |
| `Logic/Utils/IPreferencesFacade.cs` | `Logic/Layers/IPreferencesFacade.cs` | 36 | Relocate + Namespace update |

**Total Relocated**: 18 files, ~1,778 lines

### Decompositions

#### DrawingStorageMomento (541 lines) → 10 Files

| Old Method/Section | New File | Lines | Responsibility |
|--------------------|----------|-------|----------------|
| Interface | `Logic/Storage/IDrawingStorage.cs` | 30 | Redesigned interface |
| Constructor, fields | `Logic/Storage/DrawingStorageConfiguration.cs` | 40 | Shared configuration |
| `LoadAllDrawingsAsync()` | `Logic/Storage/LoadAllDrawingsHandler.cs` | 40 | Load all drawings |
| `LoadDrawingAsync()` | `Logic/Storage/LoadDrawingHandler.cs` | 30 | Load single drawing |
| `ExternalDrawingAsync()` + `GetNextDefaultNameAsync()` | `Logic/Storage/SaveDrawingHandler.cs` | 45 | Save drawing |
| `DeleteDrawingAsync()` | `Logic/Storage/DeleteDrawingHandler.cs` | 25 | Delete drawing |
| `DuplicateDrawingAsync()` | `Logic/Storage/DuplicateDrawingHandler.cs` | 35 | Duplicate drawing |
| `RenameDrawingAsync()` + `RenameUntitledDrawingsAsync()` | `Logic/Storage/RenameDrawingHandler.cs` | 50 | Rename operations |
| `CreateExternalDrawingFromCurrent*()` + `RestoreLayers*()` + `GetBrushShapeStatic()` | `Logic/Storage/DrawingConverter.cs` | 350 | Model conversion |

**Files to Delete**: `Logic/Utils/DrawingStorageMomento.cs` (after migration complete)

#### DrawingThumbnailHandler (485 lines) → 3 Files + Extensions

| Old Method/Section | New File | Lines | Responsibility |
|--------------------|----------|-------|----------------|
| Interface (renamed) | `Logic/Canvas/IThumbnailProvider.cs` | 25 | Thumbnail provision interface |
| `GetThumbnail*()` + caching + `InvalidateThumbnailAsync()` + `ClearCacheAsync()` | `Logic/Canvas/ThumbnailProvider.cs` | 120 | Thumbnail retrieval with caching |
| `GenerateThumbnail*()` + `RenderElement()` + `RenderStamps()` | `Logic/Canvas/ThumbnailGenerator.cs` | 150 | Thumbnail generation |
| `RenderPath()` | `Logic/Extensions/SkiaSharpExtensions.cs` (extension) | 30 | Renders External.Path |
| `RenderRectangle()` | `Logic/Extensions/SkiaSharpExtensions.cs` (extension) | 42 | Renders External.Rectangle |
| `RenderEllipse()` | `Logic/Extensions/SkiaSharpExtensions.cs` (extension) | 40 | Renders External.Ellipse |
| `RenderLine()` | `Logic/Extensions/SkiaSharpExtensions.cs` (extension) | 33 | Renders External.Line |
| `GetShapePath()` | `Logic/Extensions/SkiaSharpExtensions.cs` (extension) | 40 | Converts BrushShapeType to SKPath |

**Files to Delete**:
- `Logic/Handlers/DrawingThumbnailHandler.cs` (after migration complete)
- `Logic/Handlers/IDrawingThumbnailHandler.cs` (interface renamed to IThumbnailProvider)

## Namespace Migration Map

| Old Namespace | New Namespace | Files Affected |
|---------------|---------------|----------------|
| `LunaDraw.Logic.Handlers` | `LunaDraw.Logic.Input` | CanvasInputHandler, ICanvasInputHandler |
| `LunaDraw.Logic.Handlers` | `LunaDraw.Logic.Playback` | PlaybackHandler, IPlaybackHandler, RecordingHandler, IRecordingHandler |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.Storage` | DrawingStorageMomento (decomposed), ClipboardMemento, ThumbnailCacheFacade, IThumbnailCacheFacade |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.Canvas` | DrawingThumbnailHandler (decomposed), BitmapCache, IBitmapCache |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.History` | HistoryMemento |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.Selection` | SelectionObserver |
| `LunaDraw.Logic.Utils` | `LunaDraw.Logic.Layers` | LayerFacade, ILayerFacade, QuadTreeMemento, PreferencesFacade, IPreferencesFacade |

### Import Statement Update Locations

#### ViewModels (`Logic/ViewModels/`)

| File | Old Using Statements | New Using Statements |
|------|----------------------|----------------------|
| `MainViewModel.cs` | `using LunaDraw.Logic.Handlers;`<br>`using LunaDraw.Logic.Utils;` | `using LunaDraw.Logic.Input;`<br>`using LunaDraw.Logic.Storage;`<br>`using LunaDraw.Logic.Layers;` |
| `GalleryViewModel.cs` | `using LunaDraw.Logic.Utils;` | `using LunaDraw.Logic.Storage;`<br>`using LunaDraw.Logic.Canvas;` |
| `HistoryViewModel.cs` | `using LunaDraw.Logic.Utils;` | `using LunaDraw.Logic.History;` |
| `LayerPanelViewModel.cs` | `using LunaDraw.Logic.Utils;` | `using LunaDraw.Logic.Layers;` |
| `PlaybackViewModel.cs` | `using LunaDraw.Logic.Handlers;` | `using LunaDraw.Logic.Playback;` |
| `SelectionViewModel.cs` | `using LunaDraw.Logic.Utils;` | `using LunaDraw.Logic.Selection;` |
| `ToolbarViewModel.cs` | (Check for any Handler/Utils imports) | Update as needed |

#### Pages (`Pages/`)

| File | Old Using Statements | New Using Statements |
|------|----------------------|----------------------|
| `MainPage.xaml.cs` | `using LunaDraw.Logic.Handlers;` | `using LunaDraw.Logic.Input;` |
| `PlaybackPage.xaml.cs` | `using LunaDraw.Logic.Handlers;` | `using LunaDraw.Logic.Playback;` |

#### MauiProgram.cs

**Before**:
```csharp
using LunaDraw.Logic.Handlers;
using LunaDraw.Logic.Utils;
```

**After**:
```csharp
using LunaDraw.Logic.Storage;
using LunaDraw.Logic.Input;
using LunaDraw.Logic.Canvas;
using LunaDraw.Logic.History;
using LunaDraw.Logic.Playback;
using LunaDraw.Logic.Selection;
using LunaDraw.Logic.Layers;
```

## Dependency Injection Registration Changes

### Current Registrations (MauiProgram.cs, Lines 79-98)

```csharp
// === To be updated ===
builder.Services.AddSingleton<SelectionObserver>();
builder.Services.AddSingleton<ILayerFacade, LayerFacade>();
builder.Services.AddSingleton<ICanvasInputHandler, CanvasInputHandler>();
builder.Services.AddSingleton<ClipboardMemento>();
builder.Services.AddSingleton<IBitmapCache, LunaDraw.Logic.Utils.BitmapCache>();
builder.Services.AddSingleton<IPreferencesFacade, PreferencesFacade>();
builder.Services.AddSingleton<IDrawingStorageMomento, DrawingStorageMomento>();
builder.Services.AddSingleton<LunaDraw.Logic.Services.IThumbnailCacheFacade, LunaDraw.Logic.Services.ThumbnailCacheFacade>();
builder.Services.AddSingleton<IDrawingThumbnailHandler, DrawingThumbnailHandler>();
builder.Services.AddSingleton<LunaDraw.Logic.Handlers.IRecordingHandler, LunaDraw.Logic.Handlers.RecordingHandler>();
builder.Services.AddSingleton<LunaDraw.Logic.Handlers.IPlaybackHandler, LunaDraw.Logic.Handlers.PlaybackHandler>();
```

### Updated Registrations

```csharp
// === Selection Domain ===
builder.Services.AddSingleton<SelectionObserver>();

// === Layers Domain ===
builder.Services.AddSingleton<ILayerFacade, LayerFacade>();
builder.Services.AddSingleton<IPreferencesFacade, PreferencesFacade>();

// === Input Domain ===
builder.Services.AddSingleton<ICanvasInputHandler, CanvasInputHandler>();

// === Storage Domain ===
builder.Services.AddSingleton<ClipboardMemento>();
builder.Services.AddSingleton<IDrawingStorage, DrawingStorageFacade>(); // ← Interface renamed, facade implementation
builder.Services.AddSingleton<IThumbnailCacheFacade, ThumbnailCacheFacade>();

// === Canvas Domain ===
builder.Services.AddSingleton<IBitmapCache, BitmapCache>();
builder.Services.AddSingleton<IThumbnailProvider, ThumbnailProvider>(); // ← Interface renamed
builder.Services.AddTransient<ThumbnailGenerator>(); // ← If needed for DI

// === Playback Domain ===
builder.Services.AddSingleton<IRecordingHandler, RecordingHandler>();
builder.Services.AddSingleton<IPlaybackHandler, PlaybackHandler>();

// === History Domain ===
// Note: HistoryMemento is not currently registered in DI (it may be instantiated directly or via LayerFacade)
// Verify if registration is needed
```

### Interface Renames

| Old Interface Name | New Interface Name | Reason |
|--------------------|-------------------|--------|
| `IDrawingStorageMomento` | `IDrawingStorage` | Simpler name, "Momento" suffix unnecessary |
| `IDrawingThumbnailHandler` | `IThumbnailProvider` | More accurate - provides thumbnails, not just handles them |

### Facade Implementation for DrawingStorage

**Option 1: Simple Delegation Facade**

```csharp
// Logic/Storage/DrawingStorageFacade.cs
public class DrawingStorageFacade : IDrawingStorage
{
    private readonly LoadAllDrawingsHandler loadAllHandler;
    private readonly LoadDrawingHandler loadHandler;
    private readonly SaveDrawingHandler saveHandler;
    private readonly DeleteDrawingHandler deleteHandler;
    private readonly DuplicateDrawingHandler duplicateHandler;
    private readonly RenameDrawingHandler renameHandler;
    private readonly DrawingConverter converter;

    public DrawingStorageFacade(string? storagePath = null)
    {
        var config = new DrawingStorageConfiguration(storagePath);
        loadAllHandler = new LoadAllDrawingsHandler(config);
        loadHandler = new LoadDrawingHandler(config);
        saveHandler = new SaveDrawingHandler(config);
        deleteHandler = new DeleteDrawingHandler(config);
        duplicateHandler = new DuplicateDrawingHandler(config, loadHandler, saveHandler);
        renameHandler = new RenameDrawingHandler(config, loadHandler, saveHandler);
        converter = new DrawingConverter(config);
    }

    public Task<List<External.Drawing>> LoadAllDrawingsAsync() => loadAllHandler.ExecuteAsync();
    public Task<External.Drawing?> LoadDrawingAsync(Guid id) => loadHandler.ExecuteAsync(id);
    public Task ExternalDrawingAsync(External.Drawing drawing) => saveHandler.ExecuteAsync(drawing);
    public Task DeleteDrawingAsync(Guid id) => deleteHandler.ExecuteAsync(id);
    public Task DuplicateDrawingAsync(Guid id) => duplicateHandler.ExecuteAsync(id);
    public Task RenameDrawingAsync(Guid id, string newName) => renameHandler.ExecuteAsync(id, newName);
    public Task RenameUntitledDrawingsAsync() => renameHandler.ExecuteUntitledBatchAsync();
    public External.Drawing CreateExternalDrawingFromCurrent(...) => converter.CreateExternal(...);
    public List<Layer> RestoreLayers(External.Drawing drawing) => converter.RestoreLayers(drawing);
    public Task<string> GetNextDefaultNameAsync() => saveHandler.GetNextDefaultNameAsync();
}
```

**Option 2: Handlers Registered in DI**

```csharp
// In MauiProgram.cs
builder.Services.AddSingleton<DrawingStorageConfiguration>();
builder.Services.AddSingleton<LoadAllDrawingsHandler>();
builder.Services.AddSingleton<LoadDrawingHandler>();
builder.Services.AddSingleton<SaveDrawingHandler>();
builder.Services.AddSingleton<DeleteDrawingHandler>();
builder.Services.AddSingleton<DuplicateDrawingHandler>();
builder.Services.AddSingleton<RenameDrawingHandler>();
builder.Services.AddSingleton<DrawingConverter>();
builder.Services.AddSingleton<IDrawingStorage, DrawingStorageFacade>();

// DrawingStorageFacade constructor uses DI
public DrawingStorageFacade(
    LoadAllDrawingsHandler loadAllHandler,
    LoadDrawingHandler loadHandler,
    // ... etc.
)
```

**Recommendation**: **Option 1** (Simple Delegation) to minimize DI registration verbosity. Handlers are internal implementation details of the facade.

## Class Relationship Map

### Cross-Domain Dependencies

```
Storage Domain
    ├─→ Uses: Layers Domain (for Layer models in DrawingConverter)
    └─→ Uses: Models (for External.Drawing, IDrawableElement)

Canvas Domain (ThumbnailProvider)
    ├─→ Uses: Storage Domain (IDrawingStorage to load drawings)
    └─→ Uses: Canvas Domain (ThumbnailGenerator)

Canvas Domain (ThumbnailGenerator)
    └─→ Uses: Extensions (SkiaSharpExtensions for rendering)

Input Domain
    ├─→ Uses: Layers Domain (ILayerFacade for layer management)
    ├─→ Uses: Selection Domain (SelectionObserver)
    ├─→ Uses: Models (NavigationModel, ToolContext)
    ├─→ Uses: Tools (IDrawingTool)
    ├─→ Uses: Playback Domain (IPlaybackHandler)
    └─→ Uses: ViewModels (ToolbarViewModel)

Playback Domain
    ├─→ Uses: Layers Domain (ILayerFacade)
    ├─→ Uses: Models (Layer, IDrawableElement, DrawingEvent)
    └─→ Uses: Messages (IMessageBus)

Selection Domain
    ├─→ Uses: Layers Domain (ILayerFacade)
    ├─→ Uses: Models (IDrawableElement)
    └─→ Uses: Messages (IMessageBus)

Layers Domain (LayerFacade)
    ├─→ Uses: History Domain (HistoryMemento)
    ├─→ Uses: Models (Layer)
    └─→ Uses: Messages (IMessageBus)

Layers Domain (QuadTreeMemento)
    └─→ Uses: Models (IDrawableElement)
```

### Key Insights

1. **Storage Domain** is relatively independent - only uses Models/Layers for conversion
2. **Input Domain** is the most coupled - coordinates with many domains (expected for input coordinator)
3. **Canvas Domain** uses Storage to load drawings for thumbnail generation
4. **Layers/Selection/Playback** all depend on Layers Domain (ILayerFacade is central)
5. **No circular dependencies** - all dependencies flow in one direction

## Summary Statistics

### Before Refactoring

- **Folders**: 2 (Handlers, Utils)
- **Files**: 20 files
- **Total Lines**: ~2,858 lines
- **Files > 400 lines**: 3 (DrawingStorageMomento, DrawingThumbnailHandler, CanvasInputHandler)
- **Domain Organization**: None (technical classification only)

### After Refactoring

- **Folders**: 7 domains (Storage, Input, Canvas, History, Playback, Selection, Layers)
- **Files**: 37 files (18 relocated, 19 new from decomposition)
- **Total Lines**: ~2,860 lines (slight increase due to interfaces/facades)
- **Files > 400 lines**: 0 (all decomposed or under threshold)
- **Domain Organization**: 7 cohesive domains with clear boundaries

### Lines of Code by Domain

| Domain | Files | Total Lines | Average Lines/File |
|--------|-------|-------------|-------------------|
| Storage | 12 | 753 | 63 |
| Input | 2 | 422 | 211 |
| Canvas | 5 | 415 | 83 |
| History | 1 | 86 | 86 |
| Playback | 4 | 354 | 89 |
| Selection | 1 | 142 | 142 |
| Layers | 5 | 496 | 99 |
| **Extensions (enhanced)** | 1 | ~250 | 250 |
| **Total** | **31** | **~2,918** | **94** |

**Note**: Line count increase (~60 lines) comes from new interfaces and facade boilerplate.

## Validation Checklist

- [ ] All 20 original files accounted for (relocated or decomposed)
- [ ] All new files have clear responsibilities (single responsibility)
- [ ] No files exceed 400 lines (except DrawingConverter at 350, which is acceptable)
- [ ] All domain folders have cohesive, related files
- [ ] Cross-domain dependencies are minimal and unidirectional
- [ ] DI registrations mapped for all changes
- [ ] Namespace strategy is consistent (mirrors folder structure)

---

**Data Model Status**: ✅ Complete - Ready for contracts generation
