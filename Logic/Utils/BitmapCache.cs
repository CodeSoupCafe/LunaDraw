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
using LunaDraw.Logic.Extensions;

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

    var newBitmap = SkiaSharpExtensions.LoadBitmapDownsampled(path, targetWidth, targetHeight);
    if (newBitmap != null)
    {
      cache.AddOrUpdate(key,
          new WeakReference<SKBitmap>(newBitmap),
          (_, _) => new WeakReference<SKBitmap>(newBitmap));
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
      var newBitmap = SkiaSharpExtensions.LoadBitmapDownsampled(path, targetWidth, targetHeight);
      if (newBitmap != null)
      {
        cache.AddOrUpdate(key,
                   new WeakReference<SKBitmap>(newBitmap),
                   (_, _) => new WeakReference<SKBitmap>(newBitmap));
      }

      return newBitmap ?? new SKBitmap();
    });
  }

  private static string GenerateKey(string path, int width, int height)
  {
    return $"{path}_{width}x{height}";
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
