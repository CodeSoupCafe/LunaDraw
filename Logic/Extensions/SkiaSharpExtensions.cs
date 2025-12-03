using SkiaSharp;

namespace LunaDraw.Logic.Extensions
{
  public static class SkiaSharpExtensions
  {
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

    public static (SKMatrix Transform, SKRect Bounds) CalculateRotatedBounds(
        this SKMatrix canvasMatrix,
        SKPoint startPoint,
        SKPoint currentPoint)
    {
      // Calculate rotation from CanvasMatrix
      // SkewY is sin(angle) * scale, ScaleX is cos(angle) * scale
      float rotationRadians = (float)Math.Atan2(canvasMatrix.SkewY, canvasMatrix.ScaleX);
      float rotationDegrees = rotationRadians * 180f / (float)Math.PI;

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
  }
}
