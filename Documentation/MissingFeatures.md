# Missing Features & Gap Analysis

This document tracks features that are specified in the requirements or documentation but are currently missing from the implementation.

## 1. Movie Mode (Time-Lapse)

**Status:** ❌ Missing
**Requirement:** Automatically record the drawing procedure and allow playback as a short animation.
**Current State:**

- No recording logic exists in `Logic/Managers`.
- `HistoryMemento` exists for Undo/Redo but does not support time-based replay or export.

## 2. Art Management (Gallery)

**Status:** ❌ Missing
**Requirement:** Built-in gallery to store and view completed drawings and their animations.
**Current State:**

- No Gallery View or ViewModel.
- No file storage logic for saving/loading drawings (serialization).

## 4. Audio/Haptic Feedback

**Status:** ❌ Missing
**Requirement:** Immediate multi-sensory feedback (sounds, animations) for actions.
**Current State:**

- No `AudioManager` or sound services implemented in the current solution.
- References to sound exist only in `Legacy` code.

## 6. Production Deployment

**Status:** ❌ Missing
**Requirement:** Build actions for QC, smoke tests for all platforms.
**Current State:**

- No build actions exist. No docker or other deployment strategies implemented.
