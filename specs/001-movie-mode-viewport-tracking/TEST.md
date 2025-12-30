# TEST.md - Testing Strategy & Acceptance Criteria

## Testing Philosophy

This feature follows strict Test-Driven Development (TDD) with the **Red-Green-Refactor** cycle:
1. **Red**: Write failing test
2. **Green**: Write minimal code to pass test
3. **Refactor**: Improve code while keeping tests green

**Key Principles**:
- Write tests BEFORE implementation
- One assertion per line
- Test naming: `Should_{Expected}_{When}_{Condition}`
- Test instances use class name (e.g., `var playbackHandler = new PlaybackHandler()`)
- Mocks use `mock{ClassName}` convention (e.g., `mockLayerFacade`)

---

## Test Coverage Requirements

**Target**: >80% code coverage for new code

**Scope**:
- **ViewportSnapshot**: 100% coverage (simple struct)
- **ViewportInterpolator**: 100% coverage (pure functions)
- **PlaybackHandler**: >80% coverage (exclude timer edge cases)
- **DrawableElement implementations**: >80% coverage (snapshot property + clone)

---

## Unit Test Structure

### Test Organization

```
Tests/LunaDraw.Tests/
├── Features/
│   └── MovieMode/
│       ├── ViewportSnapshotTests.cs
│       ├── ViewportInterpolatorTests.cs
│       └── PlaybackHandlerViewportTests.cs
└── Models/
    └── DrawableElementViewportTests.cs
```

### Test Template

```csharp
public class ComponentNameTests
{
    // Arrange: Setup dependencies (if needed)
    private readonly Mock<IDependency> mockDependency;
    private readonly ComponentName componentName;

    public ComponentNameTests()
    {
        mockDependency = new Mock<IDependency>();
        componentName = new ComponentName(mockDependency.Object);
    }

    [Fact] // or [Theory] with [InlineData]
    public void Should_{Expected}_{When}_{Condition}()
    {
        // Arrange
        var input = CreateTestInput();

        // Act
        var result = componentName.MethodUnderTest(input);

        // Assert
        result.Should().Be(expectedValue);
        result.Property.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0f, 1.0f)] // progress: 0, expected scale: 1.0
    [InlineData(1f, 2.0f)] // progress: 1, expected scale: 2.0
    public void Should_{Expected}_{When}_{Condition}_With_Data(float progress, float expectedScale)
    {
        // Arrange
        var from = CreateFromSnapshot();
        var to = CreateToSnapshot();

        // Act
        var result = ViewportInterpolator.Lerp(from, to, progress);

        // Assert
        result.ViewMatrix.ScaleX.Should().Be(expectedScale);
    }
}
```

---

## Test Scenarios by Component

### 1. ViewportSnapshotTests

**File**: `Tests/LunaDraw.Tests/Features/MovieMode/ViewportSnapshotTests.cs`

**Purpose**: Verify snapshot creation and immutability.

| Test | Scenario | Expected Outcome |
|------|----------|------------------|
| `Should_Create_Snapshot_From_NavigationModel_When_Valid_State` | Create snapshot from NavigationModel | Matrix values copied, timestamp set |
| `Should_Return_Identity_Snapshot_When_No_Transformation` | Access Identity property | Identity matrix, MinValue timestamp |
| `Should_Preserve_Matrix_Values_When_Created` | Create snapshot directly | Values match initialization |

**Edge Cases**:
- Identity matrix (no transformation)
- Extreme scale values (0.1x, 10x)
- Large translation offsets

---

### 2. ViewportInterpolatorTests

**File**: `Tests/LunaDraw.Tests/Features/MovieMode/ViewportInterpolatorTests.cs`

**Purpose**: Verify linear interpolation correctness.

| Test | Scenario | Expected Outcome |
|------|----------|------------------|
| `Should_Return_From_Snapshot_When_Progress_Is_Zero` | Lerp with progress=0 | Returns `from` snapshot |
| `Should_Return_To_Snapshot_When_Progress_Is_One` | Lerp with progress=1 | Returns `to` snapshot |
| `Should_Interpolate_Scale_When_Progress_Is_Half` | Lerp scale with progress=0.5 | Midpoint scale |
| `Should_Interpolate_Translation_When_Progress_Is_Half` | Lerp translation with progress=0.5 | Midpoint translation |
| `Should_Clamp_Progress_When_Out_Of_Range` | Lerp with progress=-0.5 or 1.5 | Clamped to [0, 1] |

**Edge Cases**:
- Progress < 0 (should clamp to 0)
- Progress > 1 (should clamp to 1)
- From and To are identical (should return same snapshot)
- Zero scale (edge case, may cause rendering issues)

**Theory-Based Tests**:
```csharp
[Theory]
[InlineData(0f, 1.0f, 0f, 0f)]   // progress:0 → from snapshot
[InlineData(0.25f, 1.25f, 25f, 50f)]
[InlineData(0.5f, 1.5f, 50f, 100f)]
[InlineData(0.75f, 1.75f, 75f, 150f)]
[InlineData(1f, 2.0f, 100f, 200f)]  // progress:1 → to snapshot
public void Should_Interpolate_Correctly_When_Progress_Varies(
    float progress,
    float expectedScale,
    float expectedTransX,
    float expectedTransY)
{
    // Arrange
    var from = new ViewportSnapshot
    {
        ViewMatrix = SKMatrix.CreateScaleTranslation(1.0f, 1.0f, 0, 0)
    };
    var to = new ViewportSnapshot
    {
        ViewMatrix = SKMatrix.CreateScaleTranslation(2.0f, 2.0f, 100, 200)
    };

    // Act
    var result = ViewportInterpolator.Lerp(from, to, progress);

    // Assert
    result.ViewMatrix.ScaleX.Should().BeApproximately(expectedScale, 0.01f);
    result.ViewMatrix.TransX.Should().BeApproximately(expectedTransX, 0.01f);
    result.ViewMatrix.TransY.Should().BeApproximately(expectedTransY, 0.01f);
}
```

---

### 3. PlaybackHandlerViewportTests

**File**: `Tests/LunaDraw.Tests/Features/MovieMode/PlaybackHandlerViewportTests.cs`

**Purpose**: Verify playback viewport orchestration.

| Test | Scenario | Expected Outcome |
|------|----------|------------------|
| `Should_Save_Viewport_State_When_Playback_Starts` | PlayAsync() called | Current viewport saved |
| `Should_Restore_Viewport_State_When_Playback_Stops` | StopAsync() called | Viewport restored to saved state |
| `Should_Apply_First_Viewport_Snapshot_When_Playback_Starts` | PlayAsync() with snapshots | First snapshot applied to NavigationModel |
| `Should_Interpolate_Viewport_When_Element_Animates` | OnTimerTick with AnimationProgress=0.5 | Viewport interpolated |
| `Should_Use_Identity_Matrix_When_No_Snapshot_Available` | Element with null snapshot | No error, viewport unchanged |
| `Should_Use_Previous_Snapshot_When_Current_Element_Has_None` | Element[1] has null, Element[0] has snapshot | Element[0]'s snapshot used |
| `Should_Restore_Viewport_When_Playback_Completes` | Playback reaches end | Viewport restored |

**Mocking Strategy**:
```csharp
private readonly Mock<ILayerFacade> mockLayerFacade;
private readonly Mock<IMessageBus> mockMessageBus;
private readonly NavigationModel navigationModel; // Real instance
private readonly PlaybackHandler playbackHandler;

public PlaybackHandlerViewportTests()
{
    mockLayerFacade = new Mock<ILayerFacade>();
    mockMessageBus = new Mock<IMessageBus>();
    navigationModel = new NavigationModel(); // Real NavigationModel

    playbackHandler = new PlaybackHandler(
        mockLayerFacade.Object,
        mockMessageBus.Object,
        navigationModel,
        null // No dispatcher for synchronous testing
    );
}
```

**Challenge**: Testing timer-based async behavior.

**Solution**: Use `TestScheduler` (Rx) or manual timer invocation for unit tests.

**Example**:
```csharp
[Fact]
public async Task Should_Interpolate_Viewport_When_Element_Animates()
{
    // Arrange
    var firstSnapshot = new ViewportSnapshot
    {
        ViewMatrix = SKMatrix.CreateScaleTranslation(1.0f, 1.0f, 0, 0),
        Timestamp = DateTimeOffset.UtcNow
    };
    var secondSnapshot = new ViewportSnapshot
    {
        ViewMatrix = SKMatrix.CreateScaleTranslation(2.0f, 2.0f, 100, 200),
        Timestamp = DateTimeOffset.UtcNow.AddSeconds(1)
    };

    var layer = new Layer();
    var path = new DrawablePath
    {
        CreatedAt = DateTimeOffset.UtcNow,
        ViewportSnapshot = secondSnapshot,
        Path = CreateTestPath(length: 1000) // Long enough to animate
    };
    layer.Elements.Add(path);
    mockLayerFacade.Setup(x => x.Layers).Returns(new ObservableCollection<Layer> { layer });

    playbackHandler.Load(new[] { layer });

    // Act
    await playbackHandler.PlayAsync(PlaybackSpeed.Normal);

    // Simulate timer ticks manually (or use TestScheduler)
    // Set AnimationProgress to 0.5
    path.AnimationProgress = 0.5f;
    // Manually call ApplyViewportTransformation (requires reflection or test-friendly refactoring)

    // Assert
    navigationModel.ViewMatrix.ScaleX.Should().BeApproximately(1.5f, 0.1f);
    navigationModel.ViewMatrix.TransX.Should().BeApproximately(50f, 1f);
    navigationModel.ViewMatrix.TransY.Should().BeApproximately(100f, 1f);
}
```

**Note**: Timer-based tests may require architecture adjustment for testability (e.g., extract timer logic, use virtual methods, or dependency inject timer).

---

### 4. DrawableElementViewportTests

**File**: `Tests/LunaDraw.Tests/Models/DrawableElementViewportTests.cs`

**Purpose**: Verify ViewportSnapshot property on IDrawableElement implementations.

| Test | Scenario | Expected Outcome |
|------|----------|------------------|
| `Should_Allow_Null_ViewportSnapshot_When_Legacy_Element` | Create element with null snapshot | No error |
| `Should_Store_ViewportSnapshot_When_Provided` | Assign snapshot to element | Snapshot stored |
| `Should_Clone_ViewportSnapshot_When_Element_Cloned` | Clone element with snapshot | Snapshot copied |

**Test Template**:
```csharp
[Fact]
public void Should_Store_ViewportSnapshot_When_Provided()
{
    // Arrange
    var snapshot = new ViewportSnapshot
    {
        ViewMatrix = SKMatrix.CreateScaleTranslation(1.5f, 1.5f, 50, 75),
        Timestamp = DateTimeOffset.UtcNow
    };

    // Act
    var path = new DrawablePath
    {
        ViewportSnapshot = snapshot
    };

    // Assert
    path.ViewportSnapshot.Should().Be(snapshot);
}
```

**Repeat for each IDrawableElement implementation**:
- `DrawablePath`
- `DrawableEllipse`
- `DrawableRectangle`
- `DrawableLine`
- `DrawableImage`
- `DrawableStamps`
- `DrawableGroup`

---

## Integration Tests

### End-to-End Playback Test

**Scenario**: Create drawing with viewport snapshots, play Movie Mode, verify viewport follows.

**Test**:
```csharp
[Fact]
public async Task Should_Follow_Viewport_Transformations_When_Playing_Movie_Mode()
{
    // Arrange
    var navigationModel = new NavigationModel();
    var layerFacade = CreateRealLayerFacade();
    var messageBus = CreateRealMessageBus();
    var playbackHandler = new PlaybackHandler(layerFacade, messageBus, navigationModel);

    // Create drawing with varied viewport snapshots
    var layer = new Layer();

    var path1 = new DrawablePath
    {
        CreatedAt = DateTimeOffset.UtcNow,
        ViewportSnapshot = new ViewportSnapshot
        {
            ViewMatrix = SKMatrix.CreateScaleTranslation(1.0f, 1.0f, 0, 0),
            Timestamp = DateTimeOffset.UtcNow
        },
        Path = CreateTestPath()
    };

    var path2 = new DrawablePath
    {
        CreatedAt = DateTimeOffset.UtcNow.AddSeconds(1),
        ViewportSnapshot = new ViewportSnapshot
        {
            ViewMatrix = SKMatrix.CreateScaleTranslation(2.0f, 2.0f, 100, 200),
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(1)
        },
        Path = CreateTestPath()
    };

    layer.Elements.Add(path1);
    layer.Elements.Add(path2);
    layerFacade.AddLayer(layer);

    // Act
    playbackHandler.Load(new[] { layer });
    await playbackHandler.PlayAsync(PlaybackSpeed.Normal);

    // Assert (requires waiting for timer ticks or using TestScheduler)
    // Verify viewport changes from path1's snapshot to path2's snapshot
}
```

**Challenge**: Async timer-based testing.

**Solution**: Use `TestScheduler` or manual tick invocation.

---

### Serialization Roundtrip Test

**Scenario**: Save drawing with snapshots, load, verify snapshots preserved.

**Test**:
```csharp
[Fact]
public void Should_Preserve_ViewportSnapshot_When_Save_And_Load_Roundtrip()
{
    // Arrange
    var originalSnapshot = new ViewportSnapshot
    {
        ViewMatrix = SKMatrix.CreateScaleTranslation(1.5f, 1.5f, 50, 75),
        Timestamp = DateTimeOffset.Parse("2025-12-30T00:00:00Z")
    };

    var layer = new Layer();
    var path = new DrawablePath
    {
        CreatedAt = DateTimeOffset.UtcNow,
        ViewportSnapshot = originalSnapshot,
        Path = CreateTestPath()
    };
    layer.Elements.Add(path);

    var storage = new DrawingStorageMomento();

    // Act
    var json = storage.Serialize(new[] { layer });
    var loadedLayers = storage.Deserialize(json);

    // Assert
    var loadedPath = loadedLayers.First().Elements.First() as DrawablePath;
    loadedPath.Should().NotBeNull();
    loadedPath!.ViewportSnapshot.Should().NotBeNull();
    loadedPath.ViewportSnapshot!.Value.ViewMatrix.ScaleX.Should().Be(1.5f);
    loadedPath.ViewportSnapshot!.Value.ViewMatrix.TransX.Should().Be(50f);
}
```

---

## Manual Testing Checklist

### Test Case 1: New Drawing with Viewport Tracking

**Steps**:
1. Launch LunaDraw
2. Create new drawing
3. Draw path at 100% zoom (identity matrix)
4. Zoom in to 200% (scale=2.0)
5. Draw detail path
6. Zoom out to 50% (scale=0.5)
7. Draw background path
8. Click "Play Movie Mode"

**Expected Result**:
- Playback starts at 100% zoom
- Viewport zooms in to 200% as detail path animates
- Viewport zooms out to 50% as background path animates
- Smooth interpolation between snapshots
- After playback completes, viewport returns to state before playback

---

### Test Case 2: Legacy Drawing (No Viewport Data)

**Steps**:
1. Load drawing created before this feature (no viewport snapshots)
2. Click "Play Movie Mode"

**Expected Result**:
- Playback runs without errors
- Viewport remains static throughout playback
- No viewport jumps or glitches

---

### Test Case 3: Stop/Pause/Resume

**Steps**:
1. Create drawing with viewport snapshots
2. Click "Play Movie Mode"
3. Midway through playback, click "Pause"
4. Verify viewport remains at current playback state
5. Click "Play" to resume
6. Verify playback continues from paused state
7. Click "Stop"
8. Verify viewport restores to original state

**Expected Result**:
- Pause: Viewport holds current state
- Resume: Playback continues smoothly
- Stop: Viewport restores to pre-playback state

---

### Test Case 4: Serialization Roundtrip

**Steps**:
1. Create drawing with viewport snapshots
2. Save drawing to file
3. Close app
4. Reopen app
5. Load saved drawing
6. Click "Play Movie Mode"

**Expected Result**:
- Viewport snapshots preserved in saved file
- Playback follows recorded viewport transformations
- No data loss

---

### Test Case 5: Performance (Complex Drawing)

**Steps**:
1. Create drawing with 500+ elements and varied viewport snapshots
2. Click "Play Movie Mode"
3. Monitor frame rate (use profiler or visual inspection)

**Expected Result**:
- Playback maintains 60 FPS
- No stuttering or lag
- Smooth viewport interpolation

---

## Acceptance Criteria

### Functional Criteria

| Criterion | Verification Method | Status |
|-----------|---------------------|--------|
| Viewport snapshots captured when elements created | Unit test + Manual | ☐ |
| Playback interpolates viewport smoothly | Unit test + Manual | ☐ |
| Legacy drawings play without errors | Integration test + Manual | ☐ |
| Viewport restores after playback | Unit test + Manual | ☐ |
| Stop returns to original viewport | Unit test + Manual | ☐ |
| Pause holds current viewport | Manual | ☐ |
| Resume continues from paused viewport | Manual | ☐ |
| Serialization preserves snapshots | Integration test | ☐ |

### Non-Functional Criteria

| Criterion | Verification Method | Target | Status |
|-----------|---------------------|--------|--------|
| Playback frame rate | Performance test | 60 FPS | ☐ |
| Code coverage | Coverage report | >80% | ☐ |
| Breaking changes | Regression tests | Zero | ☐ |
| Memory overhead | Profiler | <1 MB per drawing | ☐ |

### Code Quality Criteria

| Criterion | Verification Method | Status |
|-----------|---------------------|--------|
| No underscores in names | Code review | ☐ |
| No regions | Code review | ☐ |
| No abbreviations | Code review | ☐ |
| SOLID principles followed | Code review | ☐ |
| Test naming convention followed | Test review | ☐ |
| One assertion per line | Test review | ☐ |

---

## Test Execution Plan

### Phase 2: RED State
```bash
# Run all viewport-related tests (expect all to fail)
dotnet test --filter "FullyQualifiedName~Viewport"
```

**Expected**: All tests fail with `NotImplementedException`.

### Phase 3: GREEN State (Per Component)

**Step 1**: ViewportSnapshot
```bash
dotnet test --filter "FullyQualifiedName~ViewportSnapshotTests"
```
**Expected**: All ViewportSnapshotTests pass.

**Step 2**: ViewportInterpolator
```bash
dotnet test --filter "FullyQualifiedName~ViewportInterpolatorTests"
```
**Expected**: All ViewportInterpolatorTests pass.

**Step 3**: PlaybackHandler
```bash
dotnet test --filter "FullyQualifiedName~PlaybackHandlerViewportTests"
```
**Expected**: All PlaybackHandlerViewportTests pass.

**Step 4**: DrawableElement
```bash
dotnet test --filter "FullyQualifiedName~DrawableElementViewportTests"
```
**Expected**: All DrawableElementViewportTests pass.

**Step 5**: All Tests
```bash
dotnet test Tests/LunaDraw.Tests/LunaDraw.Tests.csproj
```
**Expected**: All tests pass (existing + new).

### Phase 4: Coverage Report
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```
**Expected**: >80% coverage for new code.

---

## Known Testing Challenges

### Challenge 1: Timer-Based Async Behavior

**Issue**: `PlaybackHandler` uses `IDispatcherTimer` with async event handlers, making synchronous unit tests difficult.

**Solutions**:
1. **Manual Tick Invocation**: Expose timer tick for testing (not ideal for production code)
2. **TestScheduler**: Use Rx TestScheduler to control time (requires refactoring)
3. **Integration Tests**: Accept that some behavior requires integration testing

**Decision**: Use integration tests for timer-based scenarios; unit test individual methods (e.g., `ApplyViewportTransformation`).

### Challenge 2: Reflection for Private Methods

**Issue**: `ApplyViewportTransformation` is private, hard to unit test directly.

**Solutions**:
1. **Make Internal**: Use `[assembly: InternalsVisibleTo("LunaDraw.Tests")]`
2. **Test Indirectly**: Verify behavior through public API (`PlayAsync`, `StopAsync`)
3. **Refactor**: Extract logic to separate testable class

**Decision**: Test indirectly through public API for MVP; refactor in Phase 4 if needed.

### Challenge 3: SkiaSharp Matrix Comparison

**Issue**: `SKMatrix` equality may have floating-point precision issues.

**Solution**: Use `FluentAssertions` with tolerance:
```csharp
result.ViewMatrix.ScaleX.Should().BeApproximately(expectedScale, precision: 0.01f);
```

---

## Test Data Helpers

**Utility Methods**:
```csharp
public static class TestDataHelpers
{
    public static SKPath CreateTestPath(float length = 100)
    {
        var path = new SKPath();
        path.MoveTo(0, 0);
        path.LineTo(length, 0); // Horizontal line
        return path;
    }

    public static ViewportSnapshot CreateSnapshot(float scale, float transX, float transY)
    {
        return new ViewportSnapshot
        {
            ViewMatrix = SKMatrix.CreateScaleTranslation(scale, scale, transX, transY),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public static DrawablePath CreatePathWithSnapshot(ViewportSnapshot snapshot)
    {
        return new DrawablePath
        {
            Path = CreateTestPath(),
            CreatedAt = DateTimeOffset.UtcNow,
            ViewportSnapshot = snapshot,
            StrokeColor = SKColors.Black,
            StrokeWidth = 5
        };
    }
}
```

---

## Summary

This testing strategy ensures robust implementation of Movie Mode viewport tracking:
- **Unit Tests**: Verify individual components in isolation
- **Integration Tests**: Verify end-to-end behavior
- **Manual Tests**: Verify user-facing scenarios
- **Performance Tests**: Verify 60 FPS requirement
- **Acceptance Criteria**: Clear definition of "done"

**Success = All tests green + Acceptance criteria met + No breaking changes**
