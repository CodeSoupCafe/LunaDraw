# Feature Specification: Movie Mode (Playback)

**Feature Branch**: `001-movie-mode-playback`
**Created**: 2025-12-23
**Status**: Draft
**Input**: User description: "As a user I would like to automatically record the drawing procedure and allow playback as a short animation."

## User Scenarios & Testing

### User Story 1 - Automatic Recording (Priority: P1)

The system automatically captures every stroke and action as the child draws, without requiring any manual setup or activation.

**Why this priority**: Without data recording, playback is impossible. This is the foundational data ingestion requirement.

**Independent Test**: Can be tested by drawing various shapes and verifying that a sequence of drawing events is persisted in memory/storage, even without the UI to play it back.

**Acceptance Scenarios**:

1. **Given** a new blank canvas, **When** the user draws multiple strokes (lines, shapes, stamps), **Then** the system records each addition sequentially with its properties (color, type, position).
2. **Given** an existing drawing, **When** the user performs an Undo action, **Then** the recording captures the removal/undo event to ensure playback reflects the correction.
3. **Given** a complex drawing session, **When** the user saves the drawing, **Then** the recorded history is saved alongside the final image.

---

### User Story 2 - In-App Playback (Priority: P2)

The user can tap a "Play" button to watch their drawing recreate itself magically on screen.

**Why this priority**: This delivers the user-facing value ("Movie Mode") requested.

**Independent Test**: Can be tested by loading a drawing with known history and triggering playback, verifying the visual sequence matches the creation order.

**Acceptance Scenarios**:

1. **Given** a drawing with history, **When** the user taps the "Play" button, **Then** the canvas clears and rapidly redraws each stroke in the order they were created.
2. **Given** a playback in progress, **When** the playback finishes, **Then** the final state of the drawing is restored and editable.
3. **Given** a playback in progress, **When** the user taps anywhere, **Then** the playback stops and the full drawing is immediately shown (skip to end).

## Clarifications

### Session 2025-12-23
- Q: Should playback speed be capped, a fixed multiplier, or fixed speed? → A: Fixed speed with adjustable presets (Slow, Quick, Fast).
- Q: Should playback show full history (including undos) or a clean reconstruction? → A: Clean reconstruction (only play back elements that exist in the final drawing).
- Q: Should playback be accessible from the Canvas, Gallery, or both? → A: Both.
- Q: Should the canvas clear immediately or have a transition before playback? → A: Immediate clear.
- Q: Should playback include audio feedback/sounds as elements appear? → A: Silent playback (visual-only).

## Requirements

### Functional Requirements

- **FR-001**: System MUST automatically record all canvas modification events (add stroke, add shape, add stamp, clear canvas, undo/redo).
- **FR-002**: System MUST serialize and save this recording history when the drawing is saved to storage.
- **FR-003**: System MUST provide a "Play" button in both the Drawing Gallery (for saved works) and the Canvas UI (for the current session) to trigger playback.
- **FR-004**: Playback MUST animate the drawing process using a fixed speed based on user-selected presets.
- **FR-005**: Playback MUST render within the app using the existing application rendering system, NOT export a video file.
- **FR-006**: During playback, all editing tools MUST be disabled or hidden to prevent interaction conflicts.
- **FR-007**: System MUST filter the recording history to perform a "Clean Reconstruction", playing back only the elements present in the final drawing state (ignoring undone actions).
- **FR-008**: System MUST provide a simple speed selector (e.g., "Slow", "Quick", "Fast") to control playback tempo.
- **FR-009**: System MUST immediately clear the canvas to its background state when playback is initiated.

### Edge Cases

- **Interruption**: If the user backgrounds the app or receives a call during playback, playback stops and the canvas returns to the final editable state.
- **Memory Limits**: If a drawing session becomes extremely long (e.g., >10,000 strokes), the system stops recording history to prevent memory crashes, preserving the history up to that point.
- **Corrupt History**: If the saved history data is unreadable, the system fails gracefully by disabling the "Play" button for that drawing (showing only the static image).
- **Navigation**: If user navigates away (Back button) during playback, playback stops immediately.

### Key Entities

- **DrawingHistory**: An ordered list of `DrawingEvent` objects.
- **DrawingEvent**: Represents a single atomic change (Type: Add/Remove/Clear, Data: Stroke/Element, Timestamp/Order).

## Success Criteria

### Measurable Outcomes

- **SC-001**: Playback initiates within 1 second of tapping the "Play" button.
- **SC-002**: A drawing with 100 strokes completes playback in under 10 seconds (ensuring "short animation" feel).
- **SC-003**: 100% of saved drawings retain their playback history after app restart.
- **SC-004**: Playback accurately reproduces the final visual state (identical pixel/vector output) compared to the static drawing.

### Assumptions

- **Playback Location**: Playback occurs directly on the canvas view, temporarily locking user input.
- **Audio**: No background music generation required for this MVP (though "multi-sensory feedback" is a core principle, we focus on visual playback first).
- **Format**: Feature is strictly in-app playback, not video file export (MP4/GIF) at this stage.