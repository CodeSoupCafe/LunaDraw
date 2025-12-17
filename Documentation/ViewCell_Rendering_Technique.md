# ViewCell Rendering & Recycling Technique

This document details the high-performance rendering pattern used in the `SurfaceBurnCalc` project. The architecture is designed to efficiently render complex SkiaSharp vector graphics within virtualized lists (`ListView` or `CollectionView`), maximizing scrolling performance by decoupling heavy rendering logic from the lightweight cell recycling mechanism.

## Core Architecture

The system relies on a few key components interacting to solve the "heavyweight item in a list" problem:

1.  **`RenderListViewAdapter`**: Manages the collection, handles scroll events to pause rendering during fast movement, and dynamically adjusts grid spans.
2.  **`ChartZoneViewCell`**: A custom `ViewCell` that uses `OnParentSet` to inject parent-level ViewModels into recycled cells.
3.  **`RenderCanvas`**: A specialized `SKCanvasView` wrapper that handles the actual SkiaSharp drawing, scaling, and bitmap caching.
4.  **`CanvasNavigationView` & `NavigationIcon`**: specialized implementations for interactive zooming and static SVG icons, respectively.

---

## 1. The ViewCell Recycling Pattern (`ChartZoneViewCell`)

**File:** `tbsa-burn-calc/Views/Components/Charts/ChartZoneViewCell.xaml.cs`

### The Problem

In Xamarin.Forms/MAUI, `ViewCell` recycling changes the `BindingContext` to the specific data item for that row (e.g., a simple key-value pair for a burn zone). However, complex renderers often need access to the full "Page" or "ViewModel" state (e.g., the entire `ChartState` containing vector paths) which isn't present in the row data.

### The Solution: `OnParentSet` Injection

The project overrides `OnParentSet` to traverse the visual tree, locate the parent `ChartZoneView`, and manually bind the parent's ViewModel to the cell's children.

```csharp
protected override void OnParentSet()
{
    base.OnParentSet();
    SetRenderViewBindingOnParentSet();
}

private void SetRenderViewBindingOnParentSet()
{
    // 1. Find the parent Page/View (ChartZoneView) using a helper extension
    ChartZoneView? parentItem = Parent.GetParentByType<ChartZoneView>(typeof(App));

    if (parentItem != null)
    {
        // 2. Bind the heavyweight ViewModel from the parent to the child RenderCanvas controls
        // This allows the cell to access the full ChartState without passing it into every single row item.
        RenderView_Anterior.SetBinding(RenderCanvas.ViewModelProperty,
            new Binding(nameof(RenderCanvas.ViewModel), source: parentItem));

        RenderView_Posterior.SetBinding(RenderCanvas.ViewModelProperty,
            new Binding(nameof(RenderCanvas.ViewModel), source: parentItem));

        // 3. Bind Commands directly to the parent's navigation adapter
        RenderView_Anterior_Command.Command =
          new AsyncCommand<ChartZoneType>(async (zoneType) =>
          {
            await parentItem.NavigationAdapter.Push(() =>
                new ChartView(parentItem.ViewModel.ChartState, ChartPathCollectionType.Anterior, zoneType));
          });
    }
}
```

---

## 2. The Rendering Engine (`RenderCanvas`)

**File:** `tbsa-burn-calc/Views/Components/Drawing/RenderCanvas.xaml.cs`

`RenderCanvas` is the "engine" that wraps `SKCanvasView`. It is responsible for taking the vector paths (provided by the ViewModel) and drawing them effectively.

### Key Features

1.  **Coordinate Scaling**: It uses `MaxScaleCentered` to fit the arbitrary vector bounds of a body part into the specific dimensions of the cell (e.g., 120x110).
2.  **Smart Invalidation**: It listens for global `ImageLoadingState` messages. It knows when to "pause" drawing (during scroll) and when to "force redraw".
3.  **Snapshot Caching**: It can cache the rendered surface as an `SKImage` (snapshot) to prevent re-running the expensive drawing commands on every frame if the data hasn't changed.

```csharp
public void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
{
    // ... setup code ...

    // Auto-scale the canvas to fit the specific body part (ZoneType)
    surface?.Canvas?.MaxScaleCentered(
        Convert.ToInt32(args.Info.Width * 0.95),
        Convert.ToInt32(args.Info.Height * 0.95),
        bounds.Value, // The bounds of the specific vector path
        Convert.ToInt32(args.Info.Width * 0.05),
        Convert.ToInt32(args.Info.Height * 0.05),
        1);

    // Render the chart logic
    surface?.RenderChart(new RenderChartModel(ViewModel.ChartId, ViewModel.CollectionType, paths));
}
```

---

## 3. High-Performance List Management (`RenderListViewAdapter`)

**File:** `tbsa-burn-calc/Views/Components/Charts/RenderListViewAdapter.cs`

This adapter manages the `CollectionView` and implements the "Carousel" technique and scroll optimization.

### Dynamic "Carousel" Grid

The adapter calculates the available screen width and adjusts the `GridItemsLayout.Span` property. This transforms a standard list into a multi-column grid on wider devices, creating a carousel-like effect.

```csharp
public void CalculateItemWidth()
{
    var currentWidth = App.Instance?.MainPage?.Width ?? 200;
    // Calculate how many items fit (approx 200px per item)
    var spanValue = Math.Clamp(Convert.ToInt32(currentWidth / 200), 1, 99);

    // Dynamically update the Span
    ChartListView.MediaItemGridLayout.Span = spanValue;
}
```

### Scroll Debouncing (Performance)

To prevent frame drops while scrolling through complex Skia graphics, the adapter monitors scroll velocity.

1.  **Fast Scroll**: If `VerticalDelta` is large, it suppresses updates.
2.  **Stop**: When scrolling settles, it uses `.Debounce()` to trigger a `ForceRedraw` event, telling all visible `RenderCanvas` instances to render.

```csharp
public void ChartGrid_Scrolled(object sender, ItemsViewScrolledEventArgs e)
{
    refreshImagesFromScroll?.Dispose();

    // Only fire this 500ms (debounced) after scrolling STOPS
    refreshImagesFromScroll = new Action<Unit>((x) =>
    {
        _ = GlobalBroadcaster.Broadcast(AppMessageStateType.ImageLoadingState,
            new ImageLoadingState(ImageLoadingType.ForceRedraw));
    }).Debounce(ReactiveTiming.RenderCanvasLoadResetLongerDelay * 2);

    // ...
}
```

---

## 4. specialized Implementations

### NavigationIcon (SVG Rendering)

**File:** `CodeSoupCafe.Infrastructure/Views/Navigation/NavigationIcon.xaml.cs`

A lightweight implementation used for icons. It demonstrates how to recycle `SKCanvasView` for simple SVGs.

- **Input**: `Func<Stream>` (`IconSource`).
- **Recycling**: Listens to `OnBindingContextChanged` to swap the SVG source without destroying the view.
- **Drawing**: Loads the SVG stream into `SKSvg` and draws the picture.

### CanvasNavigationView (Interactive Zoom/Pan)

**File:** `tbsa-burn-calc/Views/Components/Charts/Navigation/CanvasNavigationView.xaml.cs`

Used for the detailed "Map" view of the chart.

- **Matrix Transformations**: Maintains a `CurrentMatrix` to track zoom level and offset.
- **Touch Handling**: Uses `TouchEffect` to capture pan/zoom gestures (`ZoomCanvasCommand`).
- **Dynamic Overlay**: Uses a separate `crosshairTriggered` subject to draw UI feedback (crosshairs) instantly without re-processing the underlying chart paths.

---

## Data Flow Summary

1.  **User Action**: User scrolls the `CollectionView`.
2.  **Adapter**: `RenderListViewAdapter` detects scroll, pauses rendering to keep FPS high.
3.  **Scroll Stops**: Adapter broadcasts `ImageLoadingState.ForceRedraw`.
4.  **Cell Update**: `ChartZoneViewCell` receives the broadcast. `RenderCanvas` invalidates its surface.
5.  **Binding**: `ChartZoneViewCell.OnParentSet` ensures `RenderCanvas` has the correct `ChartState`.
6.  **Rendering**: `RenderCanvas` calculates the scale for its specific `ZoneType`, draws the paths using SkiaSharp, and caches the result.
