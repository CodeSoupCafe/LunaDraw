# Spec: Smooth Path Playback

## ADDED Requirements

### Requirement: Elements MUST support progressive rendering
The system MUST allow drawing elements to be partially rendered to simulate the drawing process.

#### Scenario: Partial Path Rendering
Given a `DrawablePath` with a total length of 100 units
When `AnimationProgress` is set to 0.5
Then the `Draw` method should render the path only from the start to 50 units
And the visual output should look like a half-drawn stroke

#### Scenario: Non-Path Element Rendering
Given a `DrawableStamps` element
When `AnimationProgress` is set to 0.0
Then the element should be invisible (or 0 opacity)
When `AnimationProgress` is set to 1.0
Then the element should be fully visible

### Requirement: Playback MUST animate strokes over time
The playback engine MUST interpolate the drawing progress of paths rather than showing them instantly.

#### Scenario: Playback Loop
Given a drawing with one stroke
When Playback starts
Then the stroke should appear at `AnimationProgress = 0`
And increment towards 1.0 over several frames
And the canvas should invalidate/redraw on each frame

#### Scenario: Playback Speed
Given a long stroke and a short stroke
When Playback occurs
Then the long stroke should take proportionally longer to draw than the short stroke (constant speed)
OR
Each stroke takes a minimum fixed time (to avoid skipping short strokes)

## MODIFIED Requirements

### Requirement: Playback Sequence Sorting MUST be robust against missing timestamps
Playback sorting MUST be robust against missing timestamps by falling back to Z-Index order.

#### Scenario: Legacy Drawing Fallback
Given a set of elements with identical `CreatedAt` timestamps
When Playback orders them
Then it should respect their Z-Index (Layer List Order) to ensure the final image is composited correctly
