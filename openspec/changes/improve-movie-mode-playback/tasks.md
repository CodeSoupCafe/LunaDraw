# Tasks: Improve Movie Mode Playback

## Preparation
- [x] 1. Verify `DrawablePath` can be partially rendered using `SKPathMeasure` in a test/prototype or existing test. <!-- id: 1 -->
- [x] 2. Update `IDrawableElement` interface to include `AnimationProgress`. <!-- id: 2 -->

## Implementation
- [x] 3. Implement `AnimationProgress` in `DrawablePath` and update `Draw` method to handle partial rendering. <!-- id: 3 -->
- [x] 4. Implement `AnimationProgress` in other `IDrawableElement` implementations (defaulting to simple toggle or opacity). <!-- id: 4 -->
- [x] 5. Refactor `PlaybackHandler` to use the new progressive animation logic. <!-- id: 5 -->
    -   Implement "Animator" loop.
    -   Handle speed calculation.
- [x] 6. Ensure `StopAsync` resets all elements to `AnimationProgress = 1.0`. <!-- id: 6 -->

## Validation
- [x] 7. Verify playback with a new drawing (smooth animation). <!-- id: 7 -->
- [x] 8. Verify playback with a legacy drawing (if available) or a drawing with missing timestamps (fallback behavior). <!-- id: 8 -->
- [x] 9. Verify mixed content (Paths + Stamps). <!-- id: 9 -->
