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

namespace LunaDraw.Logic.Extensions;

public static class SkiaSharpExtensions
{
  public static SKBitmap LoadBitmapDownsampled(string path, int targetWidth, int targetHeight)
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

  public static int GetAlphaPixelCount(this SKPixmap pixmap)
  {
    return GetAlphaPixelCounts(pixmap)[0];
  }

  public static SKRect AspectFitFill(this SKRect bounds, int width, int height) =>
    width < height
    ? bounds.AspectFit(new SKSize(width, height))
    : bounds.AspectFill(new SKSize(width, height));

  public static SKPoint MapToInversePoint(this SKMatrix matrix, SKPoint point)
  {
    if (matrix.TryInvert(out var inverseMatrix))
    {
      var transformedPoint = inverseMatrix.MapPoint(point);
      return transformedPoint;
    }

    return point;
  }

  public static int[] GetAlphaPixelCounts(params SKPixmap[] pixmaps)
  {
    var totalAlphaPixels = (int[])Array.CreateInstance(typeof(int), pixmaps.Length);

    for (var xPos = 0; xPos < pixmaps[0].Width; xPos++)
      for (var yPos = 0; yPos < pixmaps[0].Height; yPos++)
        for (var pixmapCount = 0; pixmapCount < pixmaps.Length; pixmapCount++)
          totalAlphaPixels[pixmapCount] += pixmaps[pixmapCount].GetPixelColor(xPos, yPos).Alpha > 0 ? 1 : 0;

    return totalAlphaPixels;
  }

  public static SKPaint AsOpacity(this SKPaint originalPaint, byte opacity = 50)
  {
    var opacityPaint = originalPaint.Clone();
    opacityPaint.Color = new SKColor(opacityPaint.Color.Red, opacityPaint.Color.Green, opacityPaint.Color.Blue, opacity);

    return opacityPaint;
  }

  public static SKImage FlipHorizontal(this SKImage image)
  {
    if (image?.Encode()?.AsStream() is Stream imageStream)
    {
      var headerSize = 8;
      //var imageDataFlipped = ReadFully2(imageStream, image.Height, 4);
      var buffer = new byte[imageStream.Length];
      imageStream.ReadExactly(buffer, 0, (int)imageStream.Length);
      var imageDataFlipped = FlipBytesHorizontal(4, buffer.Skip(headerSize).ToArray());

      return SKImage.FromEncodedData(buffer.Take(headerSize).Concat(imageDataFlipped).ToArray());
    }

    return default!;
  }

  private static byte[] FlipBytesVertical(int size, byte[] inputArray)
  {
    byte[] reversedArray = new byte[inputArray.Length];

    for (int i = 0; i < inputArray.Length / size; i++)
    {
      Array.Copy(inputArray, reversedArray.Length - (i + 1) * size, reversedArray, i * size, size);
    }

    return reversedArray;
  }

  private static byte[] FlipBytesHorizontal(int size, byte[] inputArray)
  {
    byte[] reversedArray = new byte[inputArray.Length];

    for (int i = 0; i < inputArray.Length / size; i++)
      for (int j = 0; j < size; j++)
        reversedArray[i * size + j] = inputArray[(i + 1) * size - j - 1];

    return reversedArray;
  }

  public static byte[] ReadFully2(Stream stream, int rowCount, int bytesPerRow)
  {
    byte[] imageInfo = new byte[rowCount * bytesPerRow];

    int i = (rowCount - 1) * bytesPerRow; // get the index of the last row in the image

    while (i >= 0)
    {
      stream.ReadExactly(imageInfo, i, bytesPerRow);
      i -= bytesPerRow;
    }

    return imageInfo;
  }

  private static byte[] ReverseFrameInPlace2(int stride, byte[] framePixels)
  {
    var reversedFramePixels = new byte[framePixels.Length];
    var lines = framePixels.Length / stride;

    for (var line = 0; line < lines; line++)
    {
      Array.Copy(framePixels, framePixels.Length - ((line + 1) * stride), reversedFramePixels, line * stride, stride);
    }

    return reversedFramePixels;
  }

  public static float GetRotationDegrees(this SKMatrix matrix)
  {
    // SkewY is sin(angle) * scale, ScaleX is cos(angle) * scale
    float rotationRadians = (float)Math.Atan2(matrix.SkewY, matrix.ScaleX);
    return rotationRadians * 180f / (float)Math.PI;
  }

  public static (SKMatrix Transform, SKRect Bounds) CalculateRotatedBounds(
      this SKMatrix canvasMatrix,
      SKPoint startPoint,
      SKPoint currentPoint)
  {
    // Calculate rotation from CanvasMatrix
    float rotationDegrees = canvasMatrix.GetRotationDegrees();

    // Create alignment matrices
    var toAligned = SKMatrix.CreateRotationDegrees(rotationDegrees);
    var toWorld = SKMatrix.CreateRotationDegrees(-rotationDegrees);

    var p1 = toAligned.MapPoint(startPoint);
    var p2 = toAligned.MapPoint(currentPoint);

    var left = Math.Min(p1.X, p2.X);
    var top = Math.Min(p1.Y, p2.Y);
    var right = Math.Max(p1.X, p2.X);
    var bottom = Math.Max(p1.Y, p2.Y);

    var width = right - left;
    var height = bottom - top;

    // The Top-Left corner in aligned space
    var alignedTL = new SKPoint(left, top);

    // Transform aligned Top-Left back to World space
    var worldTL = toWorld.MapPoint(alignedTL);

    // Assemble transform: Translate to World TL, then Rotate by -Degrees (which matches toWorld)
    var translation = SKMatrix.CreateTranslation(worldTL.X, worldTL.Y);

    var transformMatrix = SKMatrix.Concat(translation, toWorld);
    var bounds = new SKRect(0, 0, width, height);

    return (transformMatrix, bounds);
  }

  public static SKMatrix MaxScaleCentered(this SKCanvas canvas,
    int width,
    int height,
    SKRect bounds,
    float imageX = 0,
    float imageY = 0,
    float imageScale = 1)
  {
    canvas.Translate(width / 2f, height / 2f);

    var ratio = bounds.Width < bounds.Height
      ? height / bounds.Height
      : width / bounds.Width;

    canvas.Scale(ratio);
    canvas.Translate(-bounds.MidX + imageX, -bounds.MidY + imageY);

    if (imageScale != 1)
      canvas.Scale(imageScale);

    return canvas.TotalMatrix;
  }

  public static string ToHex(this SKColor color, bool includeAlpha = true)
  {
      if (includeAlpha)
      {
          return $"#{color.Alpha:X2}{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
      }
      else
      {
          return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
      }
  }

  public static SKColor ToSKColor(this string hex)
  {
      if (string.IsNullOrEmpty(hex))
      {
          return SKColors.Transparent;
      }

      // Remove # if present
      if (hex.StartsWith("#"))
      {
          hex = hex.Substring(1);
      }

      if (hex.Length == 6) // RGB
      {
          return SKColor.Parse(hex);
      }
      else if (hex.Length == 8) // ARGB
      {
          return SKColor.Parse(hex);
      }
      
      // Default to transparent if parsing fails
      return SKColors.Transparent;
  }
}
