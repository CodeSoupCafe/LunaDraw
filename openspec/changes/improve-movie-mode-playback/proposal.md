# Proposal: Improve Movie Mode Playback

## What Changes
Refactor the "Movie Mode" (Playback) feature to provide a true "replay" experience where strokes are drawn progressively (animated) rather than appearing instantly. This addresses the feedback that the current implementation "pops" elements in and doesn't show the actual drawing process.

## Why
The current `PlaybackHandler` implementation iterates through all elements in the drawing and adds them to the canvas one by one. This results in a slideshow-like effect. Users expect to see the strokes being drawn as if a ghost were drawing them. Additionally, there are issues with playback consistency across different drawings, likely due to how `LayerFacade` state is managed during playback.

## Goals
1.  **Stroke Animation:** Animate `DrawablePath` elements so they appear to be drawn over time (start to finish) based on their length.
2.  **Robustness:** Ensure playback works reliably for both new sessions and loaded drawings, regardless of whether they have granular history data (falling back to visual order).
3.  **Consistency:** Ensure non-path elements (Stamps, Shapes) appear gracefully (pop-in or fade).

## Non-Goals
-   Real-time playback speed (recording actual time between events). We will use a constant drawing speed (pixels per second) or duration per stroke for a smoother "movie" feel.
-   Exporting video files (this is strictly in-app playback for now).

## Risks
-   **Performance:** `SKPathMeasure` and partial path rendering every frame could be expensive if not optimized.
-   **State Management:** Modifying elements to support "partial drawing" might leak into the main canvas state if not reset properly.
