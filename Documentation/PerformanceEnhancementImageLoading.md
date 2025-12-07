# SkiaSharp Performance Issues with Large Images

The performance problems when loading large images in SkiaSharp, especially with OpenGL rendering (SKGLView), stem from several fundamental issues:

## Why Performance Is Awful

**Memory Overhead**
When you load a large image (say, a 4000x3000 pixel photo), SkiaSharp decodes the entire image into uncompressed bitmap data in RAM. A 12MP image becomes approximately 48MB of memory (4000 × 3000 × 4 bytes per RGBA pixel). If you're loading multiple images or repeatedly loading/disposing them, you'll trigger garbage collection storms that freeze your UI.

**GPU Texture Upload Bottleneck**
With SKGLView, the decoded bitmap must be uploaded to GPU memory as a texture. This CPU-to-GPU transfer is slow, especially for large textures. Many mobile GPUs have maximum texture size limits (often 4096x4096), and large textures consume significant VRAM. If you exceed these limits, performance degrades catastrophically or rendering fails entirely.

**Repeated Decoding**
If you're not caching decoded bitmaps properly, SkiaSharp will re-decode images on every frame or draw call. JPEG/PNG decoding is CPU-intensive, and doing this repeatedly kills performance.

**Lack of Downsampling**
Loading a 4000x3000 image to display in a 400x300 view wastes enormous resources. You're decoding, storing, and uploading 10x more data than necessary.

## Best Practices and Techniques

**1. Downsample During Decode**

Always decode images at the size you actually need, not their full resolution:

```csharp
using (var stream = File.OpenRead(imagePath))
{
    using (var codec = SKCodec.Create(stream))
    {
        var info = codec.Info;
        
        // Calculate target size (e.g., fit to view dimensions)
        var targetWidth = 800;
        var scale = targetWidth / (float)info.Width;
        var targetHeight = (int)(info.Height * scale);
        
        var targetInfo = new SKImageInfo(targetWidth, targetHeight);
        using (var bitmap = SKBitmap.Decode(codec, targetInfo))
        {
            // Use bitmap...
        }
    }
}
```

**2. Use Image Caching**

Create a cache to avoid re-decoding:

```csharp
private readonly Dictionary<string, SKBitmap> _bitmapCache = new();

public SKBitmap GetCachedBitmap(string path, int maxWidth)
{
    var cacheKey = $"{path}_{maxWidth}";
    
    if (!_bitmapCache.ContainsKey(cacheKey))
    {
        _bitmapCache[cacheKey] = LoadDownsampledBitmap(path, maxWidth);
    }
    
    return _bitmapCache[cacheKey];
}
```

**3. Dispose Resources Properly**

Always dispose SKBitmap, SKImage, and SKPicture objects. Use `using` statements religiously:

```csharp
protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
{
    var canvas = e.Surface.Canvas;
    canvas.Clear(SKColors.White);
    
    using (var bitmap = GetCachedBitmap(imagePath, 1024))
    {
        canvas.DrawBitmap(bitmap, destRect);
    }
    // bitmap is disposed here, freeing GPU resources
}
```

**4. Use SKImage with GPU Backend**

For OpenGL rendering, create GPU-backed images:

```csharp
protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
{
    var canvas = e.Surface.Canvas;
    
    // Create texture-backed image from bitmap
    using (var bitmap = LoadDownsampledBitmap(path, maxSize))
    using (var image = SKImage.FromBitmap(bitmap))
    {
        canvas.DrawImage(image, destRect);
    }
}
```

**5. Implement Lazy Loading**

Don't load all images upfront. Load them on-demand:

```csharp
private SKBitmap _currentBitmap;

private void LoadImageAsync(string path)
{
    Task.Run(() =>
    {
        var bitmap = LoadDownsampledBitmap(path, targetSize);
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentBitmap?.Dispose();
            _currentBitmap = bitmap;
            skglView.InvalidateSurface();
        });
    });
}
```

**6. Use Mipmaps for Scaling**

If you're displaying images at multiple zoom levels, consider generating mipmaps:

```csharp
private List<SKBitmap> GenerateMipmaps(SKBitmap original)
{
    var mipmaps = new List<SKBitmap> { original };
    var current = original;
    
    while (current.Width > 1 && current.Height > 1)
    {
        var half = new SKBitmap(
            current.Width / 2, 
            current.Height / 2, 
            current.ColorType, 
            current.AlphaType
        );
        
        current.ScalePixels(half, SKFilterQuality.High);
        mipmaps.Add(half);
        current = half;
    }
    
    return mipmaps;
}
```

**7. Limit Texture Sizes**

Enforce maximum texture dimensions:

```csharp
private const int MAX_TEXTURE_SIZE = 2048;

private SKImageInfo CalculateSafeSize(SKImageInfo original)
{
    if (original.Width <= MAX_TEXTURE_SIZE && original.Height <= MAX_TEXTURE_SIZE)
        return original;
    
    var scale = Math.Min(
        MAX_TEXTURE_SIZE / (float)original.Width,
        MAX_TEXTURE_SIZE / (float)original.Height
    );
    
    return new SKImageInfo(
        (int)(original.Width * scale),
        (int)(original.Height * scale)
    );
}
```

**8. Avoid DrawBitmap in Hot Paths**

If drawing the same image repeatedly, cache it as an SKPicture or pre-render to a surface:

```csharp
private SKPicture _cachedPicture;

private void CacheDrawing(SKBitmap bitmap)
{
    using (var recorder = new SKPictureRecorder())
    {
        var canvas = recorder.BeginRecording(SKRect.Create(bitmap.Width, bitmap.Height));
        canvas.DrawBitmap(bitmap, 0, 0);
        _cachedPicture = recorder.EndRecording();
    }
}

protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
{
    e.Surface.Canvas.DrawPicture(_cachedPicture);
}
```

## Summary

The key to good performance with large images in SkiaSharp + OpenGL:

1. **Never load full-resolution images** - always downsample to display size
2. **Cache decoded bitmaps** - avoid repeated decoding
3. **Dispose everything** - prevent memory leaks and GPU resource exhaustion
4. **Respect GPU limits** - cap texture sizes at 2048x2048 or less
5. **Load asynchronously** - keep UI responsive
6. **Use GPU-backed resources** - leverage SKImage with OpenGL backend

Following these practices will transform your performance from unusable to smooth, even with large image galleries.