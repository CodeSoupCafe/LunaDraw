# Research: Movie Mode (Playback)

**Feature**: Movie Mode (Playback)
**Date**: 2025-12-23
**Status**: Complete

## Decision Log

### 1. Data Structure for History
**Decision**: Use a linear list of `DrawingEvent` objects stored within the `Drawing` model (or alongside it).
**Rationale**: Simple to implement and serialize. Since we need "Clean Reconstruction", the list will be filtered on save or load to remove undone actions, or we can keep the full history and filter at runtime. Given the "Clean Reconstruction" requirement, filtering at save time (or maintaining a "clean" list alongside the undo stack) is more efficient for playback.
**Approach**:
- Create `DrawingEvent` class (Type, Data, Timestamp).
- When saving, serialize this list to JSON.
- Embed in the custom `.luna` file format (likely a ZIP or custom JSON structure).

### 2. Playback Mechanism
**Decision**: `PlaybackService` driving a `DispatcherTimer` or `AnimationLoop`.
**Rationale**: MAUI's `Dispatcher.StartTimer` or an `IDispatcherTimer` is sufficient for the coarse-grained control needed here (adding elements to the canvas). We don't need a 60fps game loop for *logic*, just for *rendering* (which SkiaSharp handles).
**Speed Control**:
- **Slow**: Add 1 element every 500ms.
- **Quick**: Add 1 element every 100ms.
- **Fast**: Add 1 element every 20ms.

### 3. Rendering "Clean Reconstruction"
**Decision**: Clear canvas, then sequentially add elements from the history list to the active `Layer`.
**Rationale**: Reusing the existing `Layer` and `IDrawableElement` infrastructure ensures visual consistency. The `PlaybackService` will essentially act as a "virtual user" adding elements.
**Optimization**: For "Clean Reconstruction", we only care about the elements that survived to the end.
- *Option A*: Record every add/undo. Process list to remove undone pairs.
- *Option B*: Snapshot the final `Layer.Elements` list. This loses the *order* if the collection doesn't preserve creation order (it usually does).
- *Decision*: Use the `Layer.Elements` collection order if it preserves creation time. If Z-index manipulation changes order, we might need a separate `CreationOrder` index.
- *Refinement*: Existing `Layer` sorts by `ZIndex` for drawing. We need to persist the *creation order* separately if users can reorder layers/elements (which might change draw order but not creation time).
- *Final Decision*: Add `CreationTimestamp` to `IDrawableElement`. Sort final elements by this timestamp to reconstruct the "movie". This automatically gives us "Clean Reconstruction" (undone elements are gone) and handles reordering (we play back in time order).

### 4. Serialization
**Decision**: `System.Text.Json` with polymorphic serialization for `IDrawableElement`.
**Rationale**: Standard, fast, efficient. Need to handle abstract `IDrawableElement` types (`DrawablePath`, `DrawableShape`, etc.).

## Alternatives Considered

- **Video Export (MP4)**: Rejected per requirements (FR-005). Too heavy, requires FFmpeg or platform specific encoders.
- **Delta Compression**: Rejected. Drawings aren't large enough to warrant complex delta diffing yet.
- **Full Undo/Redo Replay**: Rejected. Requirement is "Clean Reconstruction".

## Risk Assessment

- **Large Drawings**: 10k strokes might take time to sort/deserialize. *Mitigation*: Async loading.
- **Legacy Files**: Old drawings won't have timestamps. *Mitigation*: Default to arbitrary order or index order for legacy files.
