# Carousel Component Documentation

## Overview

The Carousel component in LunaDraw (implemented via `CodeSoupCafe.Maui`'s `ItemGalleryView`) serves as the primary interface for browsing and selecting drawings. It provides a visually rich, grid-based or horizontal layout for navigating through the user's artwork.

## Benefits

1.  **Visual Discovery**: Large thumbnails allow users to quickly identify drawings visually rather than by name.
2.  **Space Efficiency**: A carousel or grid layout maximizes the number of items visible on screen compared to a traditional list.
3.  **Touch-Optimized**: Designed for swipe interactions, making it natural for touch-enabled devices.
4.  **Performance Potential**: By utilizing UI virtualization, the component can handle large collections of drawings without instantiating visual elements for off-screen items.

## Data Loading Mechanism for Infinite Scrolling

Infinite scrolling (also known as incremental loading or pagination) is a technique to improve performance by loading data only as the user needs it.

### How it Works (Conceptual/Best Practice)

1.  **Initial Load**: The ViewModel loads a small initial "page" of items (e.g., the first 20 drawings) and displays them.
2.  **Scroll Monitoring**: The underlying `CollectionView` (or `ItemGalleryView`) monitors the scroll position.
3.  **Threshold Detection**: When the user scrolls near the end of the list (defined by a `RemainingItemsThreshold`, e.g., 5 items remaining), the control triggers a command (e.g., `RemainingItemsThresholdReachedCommand`).
4.  **Incremental Fetch**: The Command invokes a method in the ViewModel to fetch the *next* page of data (e.g., items 21-40) from the storage or database.
5.  **Collection Update**: The new items are added to the existing `ObservableCollection`. The UI updates automatically to show the new items without resetting the scroll position.

### Current Implementation & Room for Improvement

**Current State**:
Currently, `LunaDraw`'s `DrawingGalleryPopupViewModel.LoadDrawingsAsync` loads **all** drawings and generates **all** thumbnails at once when the gallery opens.

```csharp
// Current Logic
await galleryViewModel.LoadDrawingsCommand.Execute().GetAwaiter();
foreach (var drawing in galleryViewModel.Drawings) {
    // Fetches thumbnail immediately for EVERY drawing
    var thumbnail = await thumbnailService.GetThumbnailAsync(...);
    items.Add(new DrawingItemViewModel(drawing, thumbnail));
}
```

**Issues**:
*   **High Latency**: Opening the gallery becomes slower as the number of drawings increases.
*   **Memory Pressure**: Loading all thumbnails into memory at once can cause high memory usage, potentially leading to crashes on mobile devices.

**Proposed Improvements (Moving Logic to Carousel)**:
To make the component more practical and simplistic to implement, the data loading logic should be moved/refactored:

1.  **Lazy Loading**: The `DrawingGalleryPopupViewModel` should initially load only metadata (names, dates) or a small batch of full items.
2.  **Service Integration**: Move the "load next batch" logic into a dedicated service or within the `ItemGalleryView`'s ViewModel if the library supports it.
3.  **Thumbnail Virtualization**: Instead of pre-fetching all thumbnails, bind the `ImageSource` to a property that loads the thumbnail *async* only when the BindingContext is set (i.e., when the item becomes visible).

## Testing Proficiency

A test project can be implemented to verify the performance gains of infinite scrolling vs. bulk loading.

### Test Strategy
1.  **Bulk Load Scenario**: Measure time to render 1000 dummy items loaded at once.
2.  **Infinite Scroll Scenario**: Measure time to render initial 20 items, and smoothness of scrolling as more are loaded.
3.  **Memory Profiling**: Compare peak memory usage between both scenarios.

See the `tests/CarouselPerformance` project for a concrete implementation of these concepts.
