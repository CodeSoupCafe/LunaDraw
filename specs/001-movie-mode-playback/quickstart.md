# Quickstart: Testing Movie Mode

This guide verifies the "Movie Mode" feature implementation.

## Prerequisites

1. Launch the **LunaDraw** app.
2. Ensure you are on the **Main Canvas**.

## Verification Steps

### 1. Verify Recording (Data Model)
1. Draw 3 distinct shapes (e.g., Circle, Square, Triangle) in order.
2. Save the drawing.
3. *Technical Check*: Inspect the saved file (or debug output) to verify `createdAt` timestamps are present and ascending.

### 2. Verify Playback Controls
1. Locate the **"Play"** button (Toolbar or Gallery).
2. Tap "Play".
3. Verify a **Speed Selector** (Slow/Quick/Fast) appears or is accessible.

### 3. Verify Playback Behavior
1. Select "Quick".
2. Tap "Start" (or if Play starts immediately).
3. **Observe**:
    - Canvas clears immediately (White/Background).
    - Circle appears.
    - Square appears.
    - Triangle appears.
    - Playback ends, controls return to normal.

### 4. Verify "Clean Reconstruction"
1. Draw a Line.
2. Click **Undo**.
3. Draw a Circle.
4. Tap "Play".
5. **Observe**: The Line should NOT appear. Only the Circle should appear.

### 5. Verify Interruption
1. Start Playback.
2. While playing, tap the **Back** button or **Stop**.
3. **Observe**: Playback stops immediately, and the full drawing is restored.
