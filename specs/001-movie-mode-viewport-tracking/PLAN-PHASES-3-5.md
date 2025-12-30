# PLAN: Phases 3-5 (Implementation & Polish)

This document covers phases 3-5: TDD implementation loop, integration, and documentation.

**Prerequisites**: Phases 1-2 complete ([PLAN-PHASES-1-2.md](PLAN-PHASES-1-2.md)), all tests failing.

---

## Phase 3: TDD Loop (GREEN State)

**Objective**: Implement features ONE TEST AT A TIME until all tests pass.

**Duration Estimate**: 2-3 hours

### 3.1 Implement ViewportSnapshot

**Order**: Implement tests in this sequence:
1. `Should_Preserve_Matrix_Values_When_Created` (simplest - struct properties)
2. `Should_Return_Identity_Snapshot_When_No_Transformation`
3. `Should_Create_Snapshot_From_NavigationModel_When_Valid_State`

**Reference**: See [DESIGN.md](DESIGN.md) ViewportSnapshot section for full implementation.

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~ViewportSnapshotTests"
```

### 3.2 Implement ViewportInterpolator

**Order**: Implement tests in this sequence:
1. `Should_Return_From_Snapshot_When_Progress_Is_Zero`
2. `Should_Return_To_Snapshot_When_Progress_Is_One`
3. `Should_Interpolate_Scale_When_Progress_Is_Half`
4. `Should_Interpolate_Translation_When_Progress_Is_Half`
5. `Should_Clamp_Progress_When_Out_Of_Range`

**Reference**: See [DESIGN.md](DESIGN.md) ViewportInterpolator section for implementation details.

**Key Implementation Notes**:
- Use `Math.Clamp(progress, 0f, 1f)` to clamp progress
- Decompose matrices to interpolate scale and translation separately
- Reconstruct matrix using `SKMatrix.CreateScaleTranslation()`

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~ViewportInterpolatorTests"
```

### 3.3 Implement PlaybackHandler Viewport Logic

**Order**: Implement tests in this sequence:
1. `Should_Use_Identity_Matrix_When_No_Snapshot_Available` (no-op case)
2. `Should_Apply_First_Viewport_Snapshot_When_Playback_Starts`
3. `Should_Save_Viewport_State_When_Playback_Starts`
4. `Should_Restore_Viewport_State_When_Playback_Stops`
5. `Should_Restore_Viewport_When_Playback_Completes`

**Method Modifications**:

#### PrepareCanvasForPlayback
- Save current viewport using `ViewportSnapshot.FromNavigationModel(navigationModel)`
- Apply first element's snapshot if available
- Reference: [DESIGN.md](DESIGN.md) PlaybackHandler section

#### RestoreFullDrawing
- Restore saved viewport state
- Set all elements to `AnimationProgress = 1.0f`

#### ApplyViewportTransformation (New)
- Check if current element has snapshot
- Find previous snapshot by walking backward through queue
- Interpolate between snapshots based on `AnimationProgress`
- Apply to `navigationModel.ViewMatrix`

#### OnTimerTick
- Call `ApplyViewportTransformation(currentElement)` before existing animation logic

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~PlaybackHandlerViewportTests"
```

### 3.4 Implement Clone() for ViewportSnapshot

Update all `IDrawableElement` implementations to clone `ViewportSnapshot`:

**Example** (DrawablePath.Clone):
```csharp
public IDrawableElement Clone()
{
    return new DrawablePath
    {
        // ... existing properties
        ViewportSnapshot = this.ViewportSnapshot // Struct copy
    };
}
```

**Files to Update**:
- DrawablePath, DrawableEllipse, DrawableRectangle, DrawableLine
- DrawableImage, DrawableStamps, DrawableGroup

**Verification**:
```bash
dotnet test --filter "FullyQualifiedName~DrawableElementViewportTests"
```

### Final Verification

```bash
# Run ALL tests
dotnet test Tests/LunaDraw.Tests/LunaDraw.Tests.csproj
```

**Success Criteria**: All tests pass (green).

---

## Phase 4: Integration & Refactoring

**Objective**: Wire up dependency injection, test integration, and refactor for SOLID principles.

**Duration Estimate**: 1-2 hours

### 4.1 Dependency Injection (MauiProgram.cs)

**Verify** that `NavigationModel` is registered:
```csharp
builder.Services.AddSingleton<NavigationModel>();
```

No code changes needed - DI container auto-wires `NavigationModel` into `PlaybackHandler`.

### 4.2 Update Tool Implementations

Modify all drawing tools to capture viewport snapshots when creating elements.

**Affected Files**:
- `Logic/Tools/FreehandTool.cs`
- `Logic/Tools/EraserBrushTool.cs`
- `Logic/Tools/LineTool.cs`
- `Logic/Tools/RectangleTool.cs`
- `Logic/Tools/EllipseTool.cs`
- `Logic/Tools/ShapeTool.cs`

**Example** (FreehandTool.OnTouchUp):
```csharp
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

**Verification**: Build and run app, draw elements, verify no runtime errors.

### 4.3 Serialization Updates

**File**: `Logic/Storage/DrawingStorageMomento.cs`

Ensure `ViewportSnapshot` is serialized/deserialized correctly (should work automatically with JSON serialization).

**Manual Test**:
1. Create drawing with varied zoom/pan
2. Save drawing
3. Close app
4. Reopen app, load drawing
5. Play Movie Mode → Viewport should follow recorded transformations

### 4.4 Refactoring for SOLID Principles

**Review**:
- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Can extend viewport interpolation without modifying core logic
- **Liskov Substitution**: `IDrawableElement` implementations remain interchangeable
- **Interface Segregation**: No unnecessary interface bloat
- **Dependency Inversion**: `PlaybackHandler` depends on `NavigationModel` abstraction (injected)

**Refactoring Opportunities**:
- Extract viewport snapshot logic into separate service if complexity grows (future)
- Add easing functions via strategy pattern (out of scope for MVP)

### 4.5 Integration Testing

**Manual Test Scenarios**:

1. **New Drawing with Viewport Tracking**
   - Create drawing with varied zoom/pan
   - Play Movie Mode
   - Verify viewport follows transformations
   - Verify smooth interpolation

2. **Legacy Drawing (No Viewport Data)**
   - Load drawing created before this feature
   - Play Movie Mode
   - Verify static viewport (no errors)

3. **Stop/Pause/Resume**
   - Start playback
   - Pause mid-way → Viewport holds current state
   - Resume → Viewport continues from paused state
   - Stop → Viewport restores to original state

4. **Serialization Roundtrip**
   - Create drawing with viewport snapshots
   - Save drawing
   - Load drawing
   - Play Movie Mode → Viewport data preserved

### 4.6 Performance Testing

**Metrics**:
- Playback maintains 60 FPS
- No noticeable lag during viewport interpolation
- Memory overhead acceptable (<1 MB for typical drawing)

**Test**: Create complex drawing (500+ elements), play Movie Mode, monitor frame rate.

### 4.7 Code Quality Checks

**Checklist**:
- ❌ No underscores in names
- ❌ No regions
- ❌ No abbreviations
- ✅ All tests follow naming convention: `Should_{Expected}_{When}_{Condition}`
- ✅ One assertion per line
- ✅ SOLID principles followed
- ✅ Code coverage >80% for new code

**Verification**:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Final Sign-Off

**Checklist**:
- ✅ All unit tests pass
- ✅ Integration tests pass
- ✅ Manual testing scenarios verified
- ✅ Performance meets requirements (60 FPS)
- ✅ No breaking changes to existing functionality
- ✅ Serialization backward-compatible
- ✅ Code follows LunaDraw coding standards

**Success Criteria**: Feature complete, all tests green, ready for user acceptance testing.

---

## Phase 5: Documentation & Cleanup (Optional)

**Objective**: Update documentation, add code comments, clean up unused code.

**Duration Estimate**: 30 minutes

### Tasks

1. **Add XML comments** to public APIs:
   - `ViewportSnapshot`
   - `ViewportInterpolator`

2. **Update README.md**:
   - Add Movie Mode viewport tracking to feature list
   - Update screenshots (if applicable)

3. **Update Documentation/Features.md**:
   - Document viewport tracking behavior
   - Add user-facing explanation

4. **Clean up debug code**:
   - Remove any temporary logging
   - Remove unused using statements

---

## Summary

**Phase 3**: Implement features one test at a time (2-3 hours)
**Phase 4**: Integration, refactoring, testing (1-2 hours)
**Phase 5**: Documentation and cleanup (30 min)

**Total Time**: 3.5-5.5 hours

**Outcome**: Fully implemented, tested, and integrated Movie Mode Viewport Tracking feature ready for production.
