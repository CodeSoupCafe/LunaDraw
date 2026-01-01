# PLAN: Phases 1-2 (Scaffold & Test Definition)

This document covers the first two phases of implementation: scaffolding and test definition.

---

## Phase 1: Scaffold (Compile-Only)

**Objective**: Create empty class/interface skeletons that compile but throw `NotImplementedException`.

**Duration Estimate**: 30 minutes

**Prerequisites**: Read [DESIGN.md](DESIGN.md) in full

### Tasks

#### 1.1 Create ViewportSnapshot Struct
**File**: `Logic/Models/ViewportSnapshot.cs`

See [DESIGN.md](DESIGN.md) for full signature. Create struct with properties and stub static methods.

#### 1.2 Create ViewportInterpolator Utility
**File**: `Logic/Utils/ViewportInterpolator.cs`

See [DESIGN.md](DESIGN.md) for signature. Create static class with `Lerp` method stub.

#### 1.3 Extend IDrawableElement Interface
**File**: `Logic/Models/IDrawableElement.cs`

Add property:
```csharp
ViewportSnapshot? ViewportSnapshot { get; set; }
```

#### 1.4 Implement ViewportSnapshot Property in All IDrawableElement Implementations

Update the following files to add the property with default `null`:
- `Logic/Models/DrawablePath.cs`
- `Logic/Models/DrawableEllipse.cs`
- `Logic/Models/DrawableRectangle.cs`
- `Logic/Models/DrawableLine.cs`
- `Logic/Models/DrawableImage.cs`
- `Logic/Models/DrawableStamps.cs`
- `Logic/Models/DrawableGroup.cs`

Example:
```csharp
public ViewportSnapshot? ViewportSnapshot { get; set; } = null;
```

#### 1.5 Update PlaybackHandler Constructor
**File**: `Logic/Handlers/PlaybackHandler.cs`

Add parameter and fields:
```csharp
private readonly NavigationModel navigationModel;
private ViewportSnapshot? savedViewportState;

public PlaybackHandler(
    ILayerFacade layerFacade,
    IMessageBus messageBus,
    NavigationModel navigationModel, // NEW
    IDispatcher? dispatcher = null)
{
    this.layerFacade = layerFacade;
    this.messageBus = messageBus;
    this.navigationModel = navigationModel; // NEW

    // ... existing code
}
```

#### 1.6 Add ApplyViewportTransformation Stub
**File**: `Logic/Handlers/PlaybackHandler.cs`

Add method:
```csharp
private void ApplyViewportTransformation(IDrawableElement currentElement)
{
    throw new NotImplementedException();
}
```

### Verification

```bash
# Build project - should compile with no errors
dotnet build LunaDraw.csproj -f net10.0-windows10.0.19041.0
```

**Success Criteria**: Project builds successfully, all new code compiles.

---

## Phase 2: Test Definition (RED State)

**Objective**: Write all failing tests based on [DESIGN.md](DESIGN.md) test scenarios. Tests should fail because implementations throw `NotImplementedException`.

**Duration Estimate**: 1-2 hours

**Prerequisites**: Phase 1 complete, [DESIGN.md](DESIGN.md) reviewed

### Test File Structure

Create test files:
- `Tests/LunaDraw.Tests/Features/MovieMode/ViewportSnapshotTests.cs`
- `Tests/LunaDraw.Tests/Features/MovieMode/ViewportInterpolatorTests.cs`
- `Tests/LunaDraw.Tests/Features/MovieMode/PlaybackHandlerViewportTests.cs`
- `Tests/LunaDraw.Tests/Models/DrawableElementViewportTests.cs`

### Test Scenarios

See [TEST.md](TEST.md) for complete test implementations. Each test file should contain:

#### ViewportSnapshotTests (3 tests)
- `Should_Create_Snapshot_From_NavigationModel_When_Valid_State`
- `Should_Return_Identity_Snapshot_When_No_Transformation`
- `Should_Preserve_Matrix_Values_When_Created`

#### ViewportInterpolatorTests (5 tests)
- `Should_Return_From_Snapshot_When_Progress_Is_Zero`
- `Should_Return_To_Snapshot_When_Progress_Is_One`
- `Should_Interpolate_Scale_When_Progress_Is_Half`
- `Should_Interpolate_Translation_When_Progress_Is_Half`
- `Should_Clamp_Progress_When_Out_Of_Range`

#### PlaybackHandlerViewportTests (5 tests)
- `Should_Save_Viewport_State_When_Playback_Starts`
- `Should_Restore_Viewport_State_When_Playback_Stops`
- `Should_Apply_First_Viewport_Snapshot_When_Playback_Starts`
- `Should_Use_Identity_Matrix_When_No_Snapshot_Available`
- `Should_Restore_Viewport_When_Playback_Completes`

#### DrawableElementViewportTests (3 tests)
- `Should_Allow_Null_ViewportSnapshot_When_Legacy_Element`
- `Should_Store_ViewportSnapshot_When_Provided`
- `Should_Clone_ViewportSnapshot_When_Element_Cloned`

### Verification

```bash
# Run tests - ALL should FAIL with NotImplementedException
dotnet test Tests/LunaDraw.Tests/LunaDraw.Tests.csproj --filter "FullyQualifiedName~Viewport"
```

**Success Criteria**: All tests fail with `NotImplementedException`.

---

## Next Steps

Once Phase 2 is complete with all tests failing (RED state), proceed to [PLAN-PHASES-3-5.md](PLAN-PHASES-3-5.md) for implementation (Phase 3).
