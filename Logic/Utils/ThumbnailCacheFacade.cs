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
 *  */

namespace LunaDraw.Logic.Services;

public class ThumbnailCacheFacade : IThumbnailCacheFacade
{
  private readonly string cachePath;

  public ThumbnailCacheFacade()
      : this(FileSystem.CacheDirectory)
  {
  }

  public ThumbnailCacheFacade(string baseCacheDirectory)
  {
    cachePath = Path.Combine(baseCacheDirectory, "thumbnails");
    Directory.CreateDirectory(cachePath);
  }

  public string GetCachePath(Guid drawingId)
  {
    return Path.Combine(cachePath, $"{drawingId}.thumb");
  }

  public async Task<string?> GetThumbnailBase64Async(Guid drawingId)
  {
    var filePath = GetCachePath(drawingId);

    if (!File.Exists(filePath))
    {
      return null;
    }

    try
    {
      return await File.ReadAllTextAsync(filePath);
    }
    catch
    {
      return null;
    }
  }

  public async Task SaveThumbnailAsync(Guid drawingId, string base64Data)
  {
    var filePath = GetCachePath(drawingId);
    await File.WriteAllTextAsync(filePath, base64Data);
  }

  public async Task InvalidateThumbnailAsync(Guid drawingId)
  {
    var filePath = GetCachePath(drawingId);

    if (File.Exists(filePath))
    {
      await Task.Run(() => File.Delete(filePath));
    }
  }

  public async Task ClearAllCacheAsync()
  {
    var files = Directory.GetFiles(cachePath, "*.thumb");

    await Task.Run(() =>
    {
      foreach (var file in files)
      {
        File.Delete(file);
      }
    });
  }
}
