# DESIGN.md - Movie Mode Viewport Tracking

## Architecture Overview

This feature extends the existing Movie Mode playback system to record and replay viewport transformations. The design follows the existing architecture patterns and introduces minimal changes to maintain stability.

### Current Architecture (Baseline)

**Existing Components**:
- `NavigationModel`: Manages viewport state via `ViewMatrix` (SKMatrix) for pan/zoom
- `IDrawableElement`: Base interface for all drawable objects, includes `CreatedAt` timestamp
- `PlaybackHandler`: Orchestrates playback, animates elements based on `AnimationProgress`
- `ILayerFacade`: Manages layers containing drawable elements
- `CanvasInputHandler`: Handles user input, delegates to active tool

**Current Playback Flow**:
1. User clicks "Play Movie Mode"
2. `PlaybackHandler.Load()` collects all elements, sorts by `CreatedAt`
3. `PlaybackHandler.PlayAsync()` starts timer (60 FPS)
4. Each tick: Update `AnimationProgress` for current element, invalidate canvas
5. Elements with `AnimationProgress < 1.0` render partially; `>= 1.0` render fully
6. On completion: Restore all elements to full visibility

### Proposed Architecture Changes

**New Components**:
- `ViewportSnapshot` (struct): Immutable snapshot of viewport state at a moment in time
- `ViewportInterpolator` (static utility): Interpolates between two `ViewportSnapshot` instances

**Modified Components**:
- `IDrawableElement`: Add optional `ViewportSnapshot?` property
- `PlaybackHandler`: Track viewport state during playback, apply interpolated transforms to `NavigationModel`

---

## Data Structures

### ViewportSnapshot (New)

**Purpose**: Immutable record of viewport state at a specific moment.

**Location**: `Logic/Models/ViewportSnapshot.cs`

**Signature**:
```csharp
namespace LunaDraw.Logic.Models;

/// <summary>
/// Immutable snapshot of viewport state at a moment in time.
/// Used for Movie Mode playback to reconstruct viewport transformations.
/// </summary>
public readonly struct ViewportSnapshot
{
    /// <summary>
    /// The view transformation matrix (pan, zoom, rotation).
    /// </summary>
    public SKMatrix ViewMatrix { get; init; }

    /// <summary>
    /// Timestamp when this snapshot was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Creates a snapshot from the current NavigationModel state.
    /// </summary>
    public static ViewportSnapshot FromNavigationModel(NavigationModel navigationModel)
    {
        return new ViewportSnapshot
        {
            ViewMatrix = navigationModel.ViewMatrix,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates an identity snapshot (no transformation).
    /// </summary>
    public static ViewportSnapshot Identity => new()
    {
        ViewMatrix = SKMatrix.CreateIdentity(),
        Timestamp = DateTimeOffset.MinValue
    };
}
```

**Test Scenarios**:
- `Should_Create_Snapshot_From_NavigationModel_When_Valid_State`
- `Should_Return_Identity_Snapshot_When_No_Transformation`
- `Should_Preserve_Matrix_Values_When_Created`

---

### ViewportInterpolator (New)

**Purpose**: Utility for smooth interpolation between viewport states.

**Location**: `Logic/Utils/ViewportInterpolator.cs`

**Signature**:
```csharp
namespace LunaDraw.Logic.Utils;

/// <summary>
/// Provides interpolation between viewport snapshots for smooth transitions.
/// </summary>
public static class ViewportInterpolator
{
    /// <summary>
    /// Linearly interpolates between two viewport snapshots.
    /// </summary>
    /// <param name="from">Starting viewport state.</param>
    /// <param name="to">Ending viewport state.</param>
    /// <param name="progress">Interpolation progress (0.0 to 1.0).</param>
    /// <returns>Interpolated viewport snapshot.</returns>
    public static ViewportSnapshot Lerp(ViewportSnapshot from, ViewportSnapshot to, float progress)
    {
        // Clamp progress to [0, 1]
        progress = Math.Clamp(progress, 0f, 1f);

        // Decompose matrices
        DecomposeMatrix(from.ViewMatrix, out var fromScale, out var fromTransX, out var fromTransY);
        DecomposeMatrix(to.ViewMatrix, out var toScale, out var toTransX, out var toTransY);

        // Interpolate components
        var scale = fromScale + (toScale - fromScale) * progress;
        var transX = fromTransX + (toTransX - fromTransX) * progress;
        var transY = fromTransY + (toTransY - fromTransY) * progress;

        // Reconstruct matrix
        var interpolatedMatrix = SKMatrix.CreateScaleTranslation(scale, scale, transX, transY);

        return new ViewportSnapshot
        {
            ViewMatrix = interpolatedMatrix,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Decomposes an SKMatrix into scale and translation components.
    /// Assumes uniform scale (ScaleX == ScaleY) and no rotation.
    /// </summary>
    private static void DecomposeMatrix(SKMatrix matrix, out float scale, out float transX, out float transY)
    {
        scale = matrix.ScaleX;
        transX = matrix.TransX;
        transY = matrix.TransY;
    }
}
```

**Test Scenarios**:
- `Should_Return_From_Snapshot_When_Progress_Is_Zero`
- `Should_Return_To_Snapshot_When_Progress_Is_One`
- `Should_Interpolate_Scale_When_Progress_Is_Half`
- `Should_Interpolate_Translation_When_Progress_Is_Half`
- `Should_Clamp_Progress_When_Out_Of_Range`

---

## Interface Modifications

### IDrawableElement (Modified)

**Change**: Add optional viewport snapshot property.

**Location**: `Logic/Models/IDrawableElement.cs`

**New Property**:
```csharp
/// <summary>
/// Optional snapshot of the viewport state when this element was created.
/// Used for Movie Mode playback to replay viewport transformations.
/// Null for legacy drawings created before viewport tracking.
/// </summary>
ViewportSnapshot? ViewportSnapshot { get; set; }
```

**Implementation Notes**:
- Default to `null` for backward compatibility
- All existing `IDrawableElement` implementations must add this property
- Serialization must handle `null` gracefully

**Test Scenarios** (per implementation):
- `Should_Allow_Null_ViewportSnapshot_When_Legacy_Element`
- `Should_Store_ViewportSnapshot_When_Provided`
- `Should_Clone_ViewportSnapshot_When_Element_Cloned`

---

## Playback Handler Modifications

### PlaybackHandler (Modified)

**Purpose**: Extend playback to apply viewport transformations during animation.

**Location**: `Logic/Handlers/PlaybackHandler.cs`

**New Fields**:
```csharp
private readonly NavigationModel navigationModel;
private ViewportSnapshot? savedViewportState; // Captured before playback starts
```

**Constructor Modification**:
```csharp
public PlaybackHandler(
    ILayerFacade layerFacade,
    IMessageBus messageBus,
    NavigationModel navigationModel, // NEW PARAMETER
    IDispatcher? dispatcher = null)
{
    this.layerFacade = layerFacade;
    this.messageBus = messageBus;
    this.navigationModel = navigationModel; // Store reference

    // ... existing timer setup
}
```

**Method Modifications**:

#### PrepareCanvasForPlayback (Modified)
```csharp
private void PrepareCanvasForPlayback()
{
    // Save current viewport state for restoration later
    savedViewportState = ViewportSnapshot.FromNavigationModel(navigationModel);

    // Reset elements to invisible/start state (existing logic)
    foreach (var element in playbackQueue)
    {
        element.AnimationProgress = 0f;
    }

    // Apply first viewport snapshot if available
    if (playbackQueue.Count > 0 && playbackQueue[0].ViewportSnapshot.HasValue)
    {
        navigationModel.ViewMatrix = playbackQueue[0].ViewportSnapshot.Value.ViewMatrix;
    }

    currentIndex = 0;
    messageBus.SendMessage(new CanvasInvalidateMessage());
}
```

#### RestoreFullDrawing (Modified)
```csharp
private void RestoreFullDrawing()
{
    // Ensure all elements are fully visible (existing logic)
    foreach (var element in playbackQueue)
    {
        element.AnimationProgress = 1.0f;
    }

    // Restore saved viewport state
    if (savedViewportState.HasValue)
    {
        navigationModel.ViewMatrix = savedViewportState.Value.ViewMatrix;
    }

    messageBus.SendMessage(new CanvasInvalidateMessage());
}
```

#### OnTimerTick (Modified)
```csharp
private async void OnTimerTick(object? sender, EventArgs e)
{
    if (currentIndex >= playbackQueue.Count)
    {
        await StopAsync();
        currentState.OnNext(PlaybackState.Completed);
        return;
    }

    var currentElement = playbackQueue[currentIndex];

    // Apply viewport interpolation if snapshots are available
    ApplyViewportTransformation(currentElement);

    // Existing animation logic for drawing elements
    bool shouldDraw = currentElement is DrawablePath || currentElement is DrawableStamps;

    if (shouldDraw)
    {
        // ... existing path animation logic
    }
    else
    {
        // ... existing pop-in logic
    }

    messageBus.SendMessage(new CanvasInvalidateMessage());
}
```

#### ApplyViewportTransformation (New)
```csharp
/// <summary>
/// Applies viewport transformation by interpolating between previous and current snapshots.
/// </summary>
private void ApplyViewportTransformation(IDrawableElement currentElement)
{
    // If current element has no snapshot, no viewport change needed
    if (!currentElement.ViewportSnapshot.HasValue)
        return;

    // Find previous element's viewport snapshot (or use identity if first element)
    ViewportSnapshot fromSnapshot = ViewportSnapshot.Identity;
    if (currentIndex > 0)
    {
        // Walk backward to find most recent snapshot
        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (playbackQueue[i].ViewportSnapshot.HasValue)
            {
                fromSnapshot = playbackQueue[i].ViewportSnapshot.Value;
                break;
            }
        }
    }

    var toSnapshot = currentElement.ViewportSnapshot.Value;

    // Interpolate based on current element's animation progress
    var interpolated = ViewportInterpolator.Lerp(
        fromSnapshot,
        toSnapshot,
        currentElement.AnimationProgress
    );

    navigationModel.ViewMatrix = interpolated.ViewMatrix;
}
```

**Test Scenarios**:
- `Should_Save_Viewport_State_When_Playback_Starts`
- `Should_Restore_Viewport_State_When_Playback_Stops`
- `Should_Apply_First_Viewport_Snapshot_When_Playback_Starts`
- `Should_Interpolate_Viewport_When_Element_Animates`
- `Should_Use_Identity_Matrix_When_No_Snapshot_Available`
- `Should_Use_Previous_Snapshot_When_Current_Element_Has_None`
- `Should_Restore_Viewport_When_Playback_Completes`

---

## Recording Viewport Snapshots

**Strategy**: Capture viewport snapshots when drawable elements are created.

**Implementation**: Modify tool implementations to capture viewport state.

### Tool Context Enhancement

**Location**: `Logic/Models/ToolContext.cs`

**Existing Field** (verify):
```csharp
public NavigationModel Navigation { get; init; }
```

**Usage in Tools**: When creating drawable elements, capture viewport snapshot.

**Example** (FreehandTool.OnTouchUp):
```csharp
// Existing code creates DrawablePath
var path = new DrawablePath
{
    Path = currentPath,
    StrokeColor = toolContext.StrokeColor,
    StrokeWidth = toolContext.StrokeWidth,
    // ... other properties
    CreatedAt = DateTimeOffset.UtcNow,
    ViewportSnapshot = ViewportSnapshot.FromNavigationModel(toolContext.Navigation) // NEW
};
```

**Affected Tools**:
- `FreehandTool`
- `EraserBrushTool`
- `LineTool`
- `RectangleTool`
- `EllipseTool`
- `ShapeTool`
- `StampTool`
- `ImageTool`

**Test Scenarios** (per tool):
- `Should_Capture_Viewport_Snapshot_When_Element_Created`
- `Should_Use_Current_NavigationModel_Matrix_When_Capturing_Snapshot`

---

## Serialization Considerations

**Requirement**: `ViewportSnapshot` must be serializable for save/load functionality.

**Location**: `Logic/Storage/DrawingStorageMomento.cs`

**Strategy**: Use nullable property, serialize as optional field.

**JSON Structure** (example):
```json
{
  "id": "guid",
  "createdAt": "2025-12-30T00:00:00Z",
  "strokeColor": "#FF5733",
  "viewportSnapshot": {
    "viewMatrix": {
      "scaleX": 1.5,
      "scaleY": 1.5,
      "transX": 100,
      "transY": 200,
      "skewX": 0,
      "skewY": 0,
      "persp0": 0,
      "persp1": 0,
      "persp2": 1
    },
    "timestamp": "2025-12-30T00:00:00Z"
  }
}
```

**Backward Compatibility**:
- If `viewportSnapshot` field is missing, property remains `null`
- Legacy drawings load correctly without viewport data

**Test Scenarios**:
- `Should_Serialize_ViewportSnapshot_When_Present`
- `Should_Deserialize_Null_ViewportSnapshot_When_Missing`
- `Should_Preserve_ViewportSnapshot_When_Save_And_Load_Roundtrip`

---

## Dependency Injection Updates

**Location**: `MauiProgram.cs`

**Change**: Inject `NavigationModel` into `PlaybackHandler`.

**Before**:
```csharp
builder.Services.AddSingleton<IPlaybackHandler, PlaybackHandler>();
```

**After**:
```csharp
// NavigationModel is already registered as Singleton
builder.Services.AddSingleton<IPlaybackHandler, PlaybackHandler>();
// PlaybackHandler constructor will receive NavigationModel via DI
```

**No code change needed** - DI container will automatically inject `NavigationModel`.

---

## Edge Cases & Error Handling

### Edge Case 1: No Viewport Snapshots (Legacy Drawings)
- **Scenario**: Playback of drawing created before this feature
- **Behavior**: Viewport remains static throughout playback
- **Implementation**: Check for `null` viewport snapshots, skip interpolation

### Edge Case 2: Partial Viewport Data
- **Scenario**: Some elements have snapshots, others don't
- **Behavior**: Interpolate when snapshots available, hold previous state otherwise
- **Implementation**: Walk backward to find last valid snapshot

### Edge Case 3: Rapid Viewport Changes
- **Scenario**: User zooms/pans rapidly while drawing
- **Behavior**: Smooth interpolation between snapshots may appear "jumpy"
- **Mitigation**: Accept as limitation for MVP; future enhancement could add easing

### Edge Case 4: Playback Stop/Pause/Resume
- **Scenario**: User stops or pauses playback mid-way
- **Behavior**: Viewport should restore to saved state on Stop, remain current on Pause
- **Implementation**:
  - Stop: Restore `savedViewportState`
  - Pause: No viewport change
  - Resume: Continue from current viewport

---

## Performance Considerations

### Viewport Updates (60 FPS)
- **Cost**: Single matrix assignment per frame (`NavigationModel.ViewMatrix = ...`)
- **Impact**: Negligible - matrix is already applied every frame during rendering
- **Optimization**: None needed for MVP

### Interpolation Calculation
- **Cost**: 3 float lerps per frame (scale, transX, transY)
- **Impact**: Negligible - simple arithmetic operations
- **Optimization**: None needed for MVP

### Memory Overhead
- **Per Element**: 1 optional `ViewportSnapshot` struct (≈48 bytes: 9 floats + timestamp)
- **Per Drawing**: ~48 bytes × element count (e.g., 500 elements = 24 KB)
- **Impact**: Minimal compared to path geometry data

---

## User Experience Flow

### Recording Flow (Drawing Session)
1. User starts drawing
2. User zooms in to add details (e.g., eyes on a face)
3. User creates several strokes → Each stroke captures current viewport
4. User zooms out to see full drawing
5. User adds background elements → Elements capture zoomed-out viewport

### Playback Flow (Movie Mode)
1. User clicks "Play Movie Mode"
2. Playback starts:
   - Canvas resets to first viewport snapshot
   - First element begins animating
   - Viewport smoothly transitions as elements appear
3. Viewer sees zoom-in effect when detail work was done
4. Viewer sees zoom-out effect when broad strokes were added
5. Playback completes:
   - Viewport returns to state before playback started
   - All elements fully visible

### Stop/Pause Behavior
- **Pause**: Viewport remains at current playback state, ready to resume
- **Stop**: Viewport restores to pre-playback state, all elements fully visible

---

## Success Criteria

### Functional Requirements
- ✅ Viewport snapshots captured when elements created
- ✅ Playback interpolates viewport smoothly between snapshots
- ✅ Legacy drawings play without errors (null snapshot handling)
- ✅ Viewport restores to original state after playback

### Non-Functional Requirements
- ✅ Maintains 60 FPS playback performance
- ✅ No breaking changes to existing serialization format
- ✅ All existing tests continue to pass
- ✅ New tests achieve >80% code coverage for new code

### User Acceptance
- ✅ Playback feels immersive and context-rich
- ✅ No jarring viewport jumps (smooth interpolation)
- ✅ Child-friendly: Simple, no configuration required

---

## Testing Strategy Summary

### Unit Tests (xUnit + Moq)
- `ViewportSnapshotTests`: Creation, identity, serialization
- `ViewportInterpolatorTests`: Lerp behavior, edge cases
- `PlaybackHandlerTests`: Viewport application, restoration, edge cases
- `DrawableElement*Tests`: Snapshot property, cloning, serialization

### Integration Tests
- End-to-end playback with viewport snapshots
- Serialization roundtrip with viewport data
- Legacy drawing compatibility

### Manual Testing
- Create drawing with varied zoom/pan → Verify playback follows viewport
- Load legacy drawing → Verify static viewport playback
- Stop/Pause/Resume → Verify viewport behavior

---

## Open Technical Questions

1. **Easing Functions**: Should interpolation use easing (ease-in/out) instead of linear?
   - **Decision**: Linear for MVP, easing in Phase 4 if time permits

2. **Snapshot Frequency**: Should snapshots be captured only on significant viewport changes?
   - **Decision**: Capture per element for simplicity; optimize if performance issues arise

3. **Matrix Decomposition**: Current implementation assumes uniform scale and no rotation. Handle rotation?
   - **Decision**: Out of scope for MVP (NavigationModel doesn't support rotation yet)

---

## Future Enhancements (Out of Scope for MVP)

- **Easing**: Add ease-in/ease-out for smoother, more cinematic viewport transitions
- **Smart Snapshots**: Only capture viewport when change exceeds threshold (reduce data size)
- **Rotation Support**: Handle rotation in viewport interpolation
- **Camera Focus**: Automatically frame current element being drawn (AI-driven camera work)
- **Export Markers**: Allow manual viewport keyframes for cinematic control
