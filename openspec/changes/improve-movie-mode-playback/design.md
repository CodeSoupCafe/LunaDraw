# Design: Progressive Playback Rendering

## Overview
To achieve "stroke-by-stroke" animation, we need to instruct `DrawablePath` to render only a fraction of itself based on the playback progress.

## Architectural Changes

### 1. `IDrawableElement` Interface
Add a property `float AnimationProgress { get; set; }` (Range 0.0 to 1.0, Default 1.0).
-   This allows the `PlaybackHandler` to control the visibility/completeness of *any* element.
-   For `DrawablePath`, it controls path length.
-   For `DrawableStamps`, it could control scale or opacity (initially just toggle visibility at 0/1).

### 2. `DrawablePath` Rendering
Modify `Draw(SKCanvas canvas)`:
-   If `AnimationProgress >= 1.0`, draw normally.
-   If `AnimationProgress < 1.0`:
    -   Use `SKPathMeasure` to calculate the total length.
    -   Calculate `segmentLength = totalLength * AnimationProgress`.
    -   Get a segment of the path (0 to `segmentLength`).
    -   Draw that segment.
    -   *Optimization:* Cache the `SKPathMeasure` or `TotalLength` if performance is an issue, but for now, doing it on-the-fly for the *active* stroke only is acceptable (only 1 stroke is animating at a time).

### 3. `PlaybackHandler` Logic
Change the timer loop to a state machine:
-   **State:** `CurrentElementIndex`, `CurrentElementProgress`.
-   **Loop (Tick):**
    -   Get `CurrentElement`.
    -   If `CurrentElement` is a Path:
        -   Increment `CurrentElementProgress` based on a fixed speed (e.g., 500px/sec).
        -   Update `CurrentElement.AnimationProgress`.
        -   `InvalidateCanvas`.
        -   If `Progress >= 1.0`, move to next element.
    -   If `CurrentElement` is not a Path:
        -   Set `AnimationProgress = 1.0`.
        -   Add to Canvas (if not already added).
        -   Move to next element.

## Handling Data Persistence
-   `CreatedAt` is already persisted.
-   We will rely on `OrderBy(CreatedAt)` as the primary sort, falling back to Z-Index (List Order) if timestamps are identical/missing. This ensures a stable playback even for legacy files.

## Safety
-   When `StopAsync` is called, we must ensure all elements have `AnimationProgress = 1.0` so the drawing returns to its correct completed state.
