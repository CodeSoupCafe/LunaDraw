# Performance Architecture

## Overview
To support complex brushes (Glow, Stamps), infinite canvas sizes, and high-performance editing, LunaDraw utilizes a three-pillared performance strategy:

1.  **GPU Acceleration** (via `SKGLView`)
2.  **Tiled Rasterization** (Layer Caching)
3.  **Spatial Partitioning** (QuadTrees)

---

## 1. GPU Acceleration
**Implementation:** `SKGLView` (OpenGL)
**Location:** `MainPage.xaml`

Instead of using the standard CPU-backed `SKCanvasView`, the application uses `SKGLView`. This moves the rendering pipeline to the device's GPU.

*   **Benefit:** Mathematical operations for rendering—specifically transparency blending, anti-aliasing, and image filters (like the Gaussian Blur used for "Glow" effects)—are orders of magnitude faster on the GPU.
*   **Impact:** Eliminates frame drops during heavy rendering passes.

## 2. Tiled Rasterization
**Implementation:** `Layer.cs` (Tile Cache)
**Location:** `Logic/Models/Layer.cs`

Rendering thousands of vector elements (paths, stamps) every frame is expensive. Instead of redrawing every vector or creating one massive bitmap for the whole layer (which consumes excessive RAM), we implement a **Tiled Cache**.

*   **Grid System:** The world is divided into **512x512 pixel tiles**.
*   **On-Demand Rendering:** Tiles are only created/rendered when they come into view.
*   **Caching:** Once a tile is rendered, it is saved as an `SKBitmap`. Subsequent frames simply draw this bitmap.
*   **Smart Invalidation:** When an element changes (moved, added, removed), only the specific tiles intersecting that element's bounding box are marked "dirty" and regenerated.

## 3. Spatial Partitioning (QuadTrees)
**Implementation:** `QuadTree<T>`
**Location:** `Logic/Utils/QuadTree.cs`, `Logic/Models/Layer.cs`

To efficiently manage thousands of objects, we use a QuadTree data structure instead of a linear list.

*   **Viewport Culling:** When rendering a specific 512x512 tile, the QuadTree allows us to query *only* the elements visible within that tile's bounds. We ignore the other 10,000 elements off-screen.
*   **Hit Testing:** Finding which object a user clicked is done in logarithmic time ($O(\log N)$) rather than linear time, keeping interaction snappy even with complex drawings.
*   **Z-Sorting:** Elements retrieved from the QuadTree are sorted by their `ZIndex` before rendering to ensure correct layering.

---

## The Rendering Pipeline

1.  **Input:** User pans/zooms the canvas.
2.  **Visibility Check:** `Layer.Draw` calculates which 512x512 tiles are visible in the current viewport.
3.  **Cache Query:**
    *   If a tile exists in `_tiles` and is clean, draw the cached bitmap.
    *   If not, trigger **RenderTile**.
4.  **RenderTile:**
    *   Query `QuadTree` for all elements intersecting this specific tile.
    *   Sort elements by `ZIndex`.
    *   Draw elements onto a new 512x512 `SKBitmap`.
    *   Store bitmap in `_tiles`.
5.  **Live Elements:**
    *   Query `QuadTree` for **Selected** elements in the viewport.
    *   Draw selected elements directly on top (bypassing the cache) to allow for smooth, high-frame-rate manipulation (dragging/resizing).
6.  **Output:** The GPU composites the tiles and live elements onto the screen.
