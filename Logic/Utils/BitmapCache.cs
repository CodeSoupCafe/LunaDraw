/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

using SkiaSharp;
using System.Collections.Concurrent;

namespace LunaDraw.Logic.Utils;

public interface IBitmapCache : IDisposable
{
  SKBitmap GetBitmap(string path, int targetWidth, int targetHeight);
  Task<SKBitmap> GetBitmapAsync(string path, int targetWidth, int targetHeight);
  void ClearCache();
}

public class BitmapCache : IBitmapCache
{
  private readonly ConcurrentDictionary<string, WeakReference<SKBitmap>> cache = new();

  public SKBitmap GetBitmap(string path, int targetWidth, int targetHeight)
  {
    var key = BitmapCache.GenerateKey(path, targetWidth, targetHeight);

    if (cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var bitmap))
    {
      return bitmap;
    }

    var newBitmap = BitmapCache.LoadDownsampledBitmap(path, targetWidth, targetHeight);
    if (newBitmap != null)
    {
      cache.AddOrUpdate(key,
          new WeakReference<SKBitmap>(newBitmap),
          (_, __) => new WeakReference<SKBitmap>(newBitmap));
    }

    return newBitmap ?? new SKBitmap();
  }

  public async Task<SKBitmap> GetBitmapAsync(string path, int targetWidth, int targetHeight)
  {
    var key = BitmapCache.GenerateKey(path, targetWidth, targetHeight);

    if (cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var bitmap))
    {
      return bitmap;
    }

    return await Task.Run(() =>
    {
      var newBitmap = BitmapCache.LoadDownsampledBitmap(path, targetWidth, targetHeight);
      if (newBitmap != null)
      {
        cache.AddOrUpdate(key,
                   new WeakReference<SKBitmap>(newBitmap),
                   (_, __) => new WeakReference<SKBitmap>(newBitmap));
      }

      return newBitmap ?? new SKBitmap();
    });
  }

  private static string GenerateKey(string path, int width, int height)
  {
    return $"{path}_{width}x{height}";
  }

  private static SKBitmap LoadDownsampledBitmap(string path, int targetWidth, int targetHeight)
  {
    try
    {
      if (!File.Exists(path))
      {
        System.Diagnostics.Debug.WriteLine($"[BitmapCache] File not found: {path}");

        return new SKBitmap();
      }

      using var stream = File.OpenRead(path);
      using var codec = SKCodec.Create(stream);

      if (codec == null)
      {
        System.Diagnostics.Debug.WriteLine($"[BitmapCache] Failed to create codec for: {path}");

        return new SKBitmap();
      }

      var info = codec.Info;

      // Calculate scale
      float scale = 1.0f;
      if (targetWidth > 0 && targetHeight > 0)
      {
        float scaleX = (float)targetWidth / info.Width;
        float scaleY = (float)targetHeight / info.Height;
        scale = Math.Min(scaleX, scaleY);
      }

      if (scale >= 1.0f || (targetWidth == 0 && targetHeight == 0))
      {
        return SKBitmap.Decode(codec);
      }

      // Get supported dimensions for this scale
      var supportedInfo = codec.GetScaledDimensions(scale);

      // Use the supported dimensions for decoding
      var decodeInfo = new SKImageInfo(supportedInfo.Width, supportedInfo.Height, info.ColorType, info.AlphaType);

      var bitmap = new SKBitmap(decodeInfo);
      var result = codec.GetPixels(decodeInfo, bitmap.GetPixels());

      if (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput)
      {
        return bitmap;
      }
      else
      {
        System.Diagnostics.Debug.WriteLine($"[BitmapCache] GetPixels failed: {result}");
        bitmap.Dispose();
        // Fallback: try full decode if downsample fails? 
        // Or maybe the scale was just invalid. 
        return new SKBitmap();
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"[BitmapCache] Exception loading bitmap: {ex}");

      return new SKBitmap();
    }
  }

  public void ClearCache()
  {
    foreach (var weakRef in cache.Values)
    {
      if (weakRef.TryGetTarget(out var bitmap))
      {
        bitmap?.Dispose();
      }
    }
    cache.Clear();
  }

  public void Dispose()
  {
    ClearCache();
  }
}
