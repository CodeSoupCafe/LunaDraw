Most of these principles apply broadly to **all SkiaSharp rendering**, not just images. Here's how:

## Universal SkiaSharp Performance Principles

**Memory Management & Disposal**
This is critical for everything\_ in SkiaSharp - not just images. Any `IDisposable` object (SKPaint, SKPath, SKShader, SKTypeface, SKSurface, etc.) must be disposed properly. Failure to do so causes:

- Memory leaks in managed memory
- Native memory leaks (SkiaSharp is a wrapper around native Skia)
- GPU resource exhaustion with OpenGL backend

```csharp
// BAD - creates new SKPaint every frame
protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
{
    var paint = new SKPaint { Color = SKColors.Red }; // Memory leak!
    canvas.DrawCircle(100, 100, 50, paint);
}

// GOOD - reuse or dispose
private readonly SKPaint paint = new SKPaint { Color = SKColors.Red };

// OR use 'using'
using (var paint = new SKPaint { Color = SKColors.Red })
{
    canvas.DrawCircle(100, 100, 50, paint);
}
```

**Caching Complex Objects**
Applies to paths, shaders, typefaces, paints with effects:

```csharp
// BAD - recreating path every frame
protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
{
    var path = new SKPath();
    path.MoveTo(0, 0);
    path.LineTo(100, 100);
    // ... complex path construction
    canvas.DrawPath(path, paint);
}

// GOOD - cache the path
private SKPath cachedPath;

private void InitializePath()
{
    cachedPath = new SKPath();
    cachedPath.MoveTo(0, 0);
    cachedPath.LineTo(100, 100);
    // ... complex path construction
}
```

**GPU Texture/Resource Limits**
Not just for images - affects:

- Large canvases/surfaces
- Complex gradients and shaders
- Many draw calls per frame
- Large paths with anti-aliasing

With SKGLView, every surface and complex effect may create GPU textures. Creating too many or making them too large causes the same bottlenecks.

**Minimize Allocations in Draw Loops**
Critical for smooth rendering at 60fps:

```csharp
// BAD - allocating every frame (causes GC pressure)
protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
{
    var rect = new SKRect(0, 0, 100, 100); // allocation
    var points = new SKPoint[] { ... }; // allocation
    canvas.DrawRect(rect, paint);
}

// GOOD - reuse or use stack allocation
private SKRect rect = new SKRect(0, 0, 100, 100);
private SKPoint[] points = new SKPoint[100];

protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
{
    canvas.DrawRect(rect, paint);
}
```

**Using SKPicture for Complex Drawings**
Works for any complex scene, not just images:

```csharp
// Cache complex vector graphics
private SKPicture CacheComplexDrawing()
{
    using (var recorder = new SKPictureRecorder())
    {
        var canvas = recorder.BeginRecording(bounds);

        // Complex drawing operations
        for (int i = 0; i < 1000; i++)
        {
            canvas.DrawCircle(x[i], y[i], radius[i], paint);
        }

        return recorder.EndRecording();
    }
}

// Then just replay the picture
canvas.DrawPicture(cachedPicture);
```

**Async Operations for Heavy Work**
Applies to any CPU-intensive operation:

- Generating complex paths
- Text layout calculations
- Building gradients
- Applying filters/effects

```csharp
private async Task<SKPath> GenerateComplexPathAsync()
{
    return await Task.Run(() =>
    {
        var path = new SKPath();
        // CPU-intensive path generation
        return path;
    });
}
```

**Layer/Surface Management**
Creating temporary surfaces is expensive:

```csharp
// BAD - creating surface every frame
using (var surface = SKSurface.Create(info))
{
    var canvas = surface.Canvas;
    // ... draw to surface
}

// GOOD - reuse surfaces when possible
private SKSurface offscreenSurface;

private void EnsureSurface(SKImageInfo info)
{
    if (offscreenSurface == null || offscreenSurface.Canvas.LocalClipBounds.Width != info.Width)
    {
        offscreenSurface?.Dispose();
        offscreenSurface = SKSurface.Create(info);
    }
}
```

## Techniques Specific to Images

These are mostly image-only:

- **Downsampling during decode** - only applies to loading images from files
- **Codec usage** - image format specific
- **Mipmaps** - primarily for images (though could apply to cached renders)

## General OpenGL/SKGLView Considerations

**Minimize State Changes**
Group similar draw calls together:

```csharp
// BAD - constant state changes
canvas.DrawCircle(x1, y1, r, redPaint);
canvas.DrawCircle(x2, y2, r, bluePaint);
canvas.DrawCircle(x3, y3, r, redPaint);

// GOOD - batch by paint/state
canvas.DrawCircle(x1, y1, r, redPaint);
canvas.DrawCircle(x3, y3, r, redPaint);
canvas.DrawCircle(x2, y2, r, bluePaint);
```

**Avoid Readback Operations**
Reading pixels from GPU to CPU is extremely slow:

```csharp
// AVOID if possible
var pixmap = surface.PeekPixels();
var pixels = pixmap.GetPixelSpan(); // GPU -> CPU transfer
```

**Use Hardware Acceleration Features**
Take advantage of GPU capabilities:

- Use shader-based effects instead of CPU pixel manipulation
- Leverage GPU-accelerated filters
- Use SKColorFilter and SKImageFilter for effects

## Bottom Line

**~80% of SkiaSharp performance best practices apply universally:**

- Proper disposal and resource management
- Caching expensive objects (paths, paints, pictures)
- Minimizing allocations in hot paths
- Avoiding repeated expensive operations
- Using async for heavy computations
- Understanding GPU resource limits with OpenGL

**~20% are image-specific:**

- Downsampling on decode
- Codec management
- Image format considerations

The performance principles for images are just more visible\_ because images are large and make the problems obvious faster. But the same issues affect all SkiaSharp rendering - they just manifest at different scales.
