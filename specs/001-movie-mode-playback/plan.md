# Implementation Plan: Movie Mode (Playback)

**Branch**: `001-movie-mode-playback` | **Date**: 2025-12-23 | **Spec**: [Link](spec.md)
**Input**: Feature specification from `/specs/001-movie-mode-playback/spec.md`

## Summary

This feature implements "Movie Mode" for LunaDraw, enabling children to watch their drawings recreate themselves as a short animation. It involves recording all canvas modification events (strokes, shapes, stamps, undos) in real-time, serializing this history with the drawing file, and providing a playback engine that reconstructs the final image by re-rendering the clean history (excluding undone actions) at a user-selectable speed.

## Technical Context

**Language/Version**: C# 12, .NET 9/10 (MAUI)
**Primary Dependencies**: SkiaSharp (rendering), ReactiveUI (MVVM), System.Text.Json (serialization)
**Storage**: File-based storage (embedded in drawing files or sidecar JSON)
**Testing**: xUnit, Moq
**Target Platform**: Windows, Android, iOS, MacCatalyst
**Project Type**: Mobile/Desktop App (.NET MAUI)
**Performance Goals**: Playback initiation < 1s, smooth rendering of 100+ strokes in < 10s.
**Constraints**: Must run on low-end mobile devices; Playback must happen on the existing CanvasView.
**Scale/Scope**: Supports drawings with thousands of strokes; Memory efficient recording.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **I. Child-Centric UX**: Simple "Play" button, visual feedback, adjustable speed presets (Slow/Quick/Fast).
- [x] **II. Reactive Architecture**: Playback state managed via ReactiveUI Observables.
- [x] **III. Test-First & Quality**: Recording and Playback logic will be unit tested independently.
- [x] **IV. SOLID & Clean Code**: Separation of concerns (Recording Logic vs. Playback Engine vs. UI).
- [x] **V. SkiaSharp & Performance**: Playback uses existing SkiaSharp rendering pipeline; "Clean Reconstruction" avoids rendering undone paths.
- [x] **VI. Architecture Patterns & Naming**: No "Service" or "Manager" suffixes. Using "Handlers" and "Facades".
- [x] **VII. SPARC Methodology**: Following Spec -> Plan -> Task flow.

## Project Structure

### Documentation (this feature)

```text
specs/001-movie-mode-playback/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (Interfaces)
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
Logic/
├── Models/
│   ├── DrawingEvent.cs          # New: Represents a single recorded action
│   ├── DrawingHistory.cs        # New: Collection of events
│   └── PlaybackState.cs         # New: State model for playback (Playing, Paused, Speed)
├── Handlers/
│   ├── IPlaybackHandler.cs      # New: Interface for playback control
│   ├── PlaybackHandler.cs       # New: Implementation of playback logic
│   ├── IRecordingHandler.cs     # New: Interface for event recording
│   └── RecordingHandler.cs      # New: Implementation of recording logic
├── ViewModels/
│   └── PlaybackViewModel.cs     # New: VM for playback controls
└── ...

Components/
├── PlaybackControls.xaml        # New: Floating controls for playback (Speed, Stop)
└── ...

tests/LunaDraw.Tests/
├── Features/
│   └── MovieMode/               # New: Feature-specific tests
│       ├── RecordingHandlerTests.cs
│       └── PlaybackHandlerTests.cs
└── ...
```

**Structure Decision**: Option 1 (Single Project) - Integrating directly into existing `LunaDraw` project structure following standard conventions.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | | |