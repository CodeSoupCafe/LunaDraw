# Matrix Translation & Zoom Mechanics in SurfaceBurnCalc

This document details the specific implementation of matrix translation, zooming, and accurate drawing coordinates found in the `Legacy/SurfaceBurnCalc` project.

## 1. Theoretical Overview

The application maintains two distinct coordinate spaces:
1.  **Screen Space:** The actual pixels on the device screen where touch events occur.
2.  **Model Space:** The coordinate system of the underlying SVG/Path data.

To draw accurately while zoomed, the application must:
1.  **Accumulate** user gestures (pan/zoom) into a transformation matrix.
2.  **Apply** this matrix to the Canvas during rendering.
3.  **Invert** this matrix when processing touch inputs to convert Screen coordinates back to Model coordinates.

---

## 2. The Core Mathematics: `TouchManipulationManager`

**File:** `Legacy/SurfaceBurnCalc/CodeSoupCafe.Xamarin.Drawing/TouchManipulationManager.cs`

This class calculates the *change* (delta) between frames. It does not store state; it only computes the matrix required to transform `prevPoint` to `newPoint`.

### One-Finger Manipulation (Panning)
Calculates a simple translation delta.

```csharp
// Lines 11-45
public SKMatrix OneFingerManipulate(SKPoint prevPoint, SKPoint newPoint, SKPoint pivotPoint, ref float rotationAngle)
{
    // ... logic for rotation if enabled ...

    // Calculate the vector difference
    SKPoint delta = newPoint - prevPoint;

    // Create a Translation Matrix for this difference
    // Line 42
    SKMatrix.PostConcat(ref touchMatrix, SKMatrix.MakeTranslation(delta.X, delta.Y));

    return touchMatrix;
}
```

### Two-Finger Manipulation (Pinch to Zoom)
Calculates scaling relative to a **pivot point**. This ensures the image zooms "under" the fingers rather than from the top-left corner.

```csharp
// Lines 47-86
public SKMatrix TwoFingerManipulate(SKPoint prevPoint, SKPoint newPoint, SKPoint pivotPoint, ref float returnAngle)
{
    // ... calculation of vectors ...

    float scaleX = Magnitude(newVector) / Magnitude(oldVector);
    float scaleY = scaleX; // Isotropic scale

    // Line 82: Create Scale Matrix centered at pivotPoint
    SKMatrix.PostConcat(ref touchMatrix,
        SKMatrix.MakeScale(scaleX, scaleY, pivotPoint.X, pivotPoint.Y));

    return touchMatrix;
}
```

---

## 3. State Management: `TouchManipulationObject`

**File:** `Legacy/SurfaceBurnCalc/tbsa-burn-calc/tbsa-burn-calc/Views/Components/Drawing/TouchManipulationObject.cs`

This class holds the state and orchestrates the rendering pipeline.

### Key Properties
*   `SKMatrix TranslateMatrix`: (Line 56) Stores the accumulated user transformations (pan/zoom/rotate) over the lifetime of the view.
*   `SKMatrix DrawMatrix`: (Line 45) Stores the **Total Transformation** currently applied to the canvas (Fit-to-Screen + User Transforms).

### Updating State (`Manipulate`)
When a touch event occurs, the manager calculates the delta and concatenates it onto the persistent `TranslateMatrix`.

```csharp
// Lines 362-422
private void Manipulate(long id, TouchActionType actionType, PandaModeType pandaMode)
{
    // ... determine points ...

    // 1. Get the Delta Matrix from the Manager
    if (infos.Length == 1)
        touchMatrix = TouchManager.OneFingerManipulate(...);
    else
        touchMatrix = TouchManager.TwoFingerManipulate(...);

    // 2. Accumulate onto the global TranslateMatrix
    // Line 421
    TranslateMatrix = TranslateMatrix.PostConcat(touchMatrix);
}
```

---

## 4. The Rendering Pipeline (`Paint`)

The `Paint` method applies the transformations to the `SKCanvas` before drawing occurs.

**Order of Operations:**
1.  **Save Canvas:** Isolate changes.
2.  **Apply User Matrix:** The `TranslateMatrix` is applied *before* the content is drawn.
3.  **Call DrawChart:** The actual drawing logic.

```csharp
// Lines 112-182
public void Paint(...)
{
    var canvas = surface.Canvas;
    canvas.Save();

    // 1. Capture the User's Accumulated Transform
    var matrix = TranslateMatrix; 

    // ...
    
    // 2. Apply the User Matrix to the Canvas
    // Line 173
    canvas.Concat(ref matrix);

    // 3. Draw the content
    // Line 175
    DrawChart(basePath, surface, imageInfo, renderChartModel, false);

    canvas.RestoreToCount(-1);
}
```

### Centering the Content (`DrawChart`)
Inside `DrawChart`, the code ensures the content fits the screen, regardless of user zooming.

```csharp
// Lines 282-306
private void DrawChart(...)
{
    // ... calculation of tightBounds ...

    // Line 297: MaxScaleCentered
    // This applies the "Fit to Screen" transform AND returns the Total Matrix
    DrawMatrix = canvas.MaxScaleCentered(imageInfo.Width, imageInfo.Height, tightBounds);

    // ... drawing occurs ...
}
```

---

## 5. Accurate Input Mapping (`MapToInversePoint`)

This is the most critical part for drawing accuracy. When the user draws a line, the raw screen coordinates must be converted to the model's coordinate space.

Since `DrawMatrix` captures the total state of the canvas at the time of drawing (Fit-to-Screen transform + User Pan/Zoom transform), its **inverse** allows mapping from Screen -> Model.

```csharp
// Lines 367-384 inside Manipulate() logic for drawing
if (inProgressPaths.TryGetValue(id, out var path))
{
    switch (actionType)
    {
        case TouchActionType.Moved:
            // Line 380
            // INVERSE MAP the screen point (newPoint) to Model Space
            path.LineTo(DrawMatrix.MapToInversePoint(newPoint));
            break;
            
        case TouchActionType.Pressed: // From ProcessTouchEvent
             // Line 202
             path.MoveTo(DrawMatrix.MapToInversePoint(location));
             break;
    }
}
```

---

## 6. Helper: `LibraryExtensions.cs`

**File:** `Legacy/SurfaceBurnCalc/CodeSoupCafe.Infrastructure/Extensions/LibraryExtensions.cs`

The `MaxScaleCentered` method is a utility that:
1.  Centers the coordinate system.
2.  Scales the canvas so the object fits within the width/height.
3.  Centers the object.
4.  **Returns `canvas.TotalMatrix`**, which allows `TouchManipulationObject` to capture the exact render state.

```csharp
// Lines 77-98
public static SKMatrix MaxScaleCentered(this SKCanvas canvas, ...)
{
    canvas.Translate(width / 2f, height / 2f); // Move to center of screen
    
    // Calculate fit ratio
    var ratio = bounds.Width < bounds.Height ? height / bounds.Height : width / bounds.Width;

    canvas.Scale(ratio); // Scale to fit
    canvas.Translate(-bounds.MidX, -bounds.MidY); // Center the object

    return canvas.TotalMatrix; // Capture the final matrix
}
```
