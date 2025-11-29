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

    /// <summary>
    ///  Draws text with a glowing effect using SkiaSharp's modern DrawText method.
    /// THIS METHOD IS UNTESTED AND MAY REQUIRE ADJUSTMENTS BASED ON YOUR SPECIFIC NEEDS.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="text"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="glowColor"></param>
    /// <param name="textColor"></param>
    /// <param name="glowRadius"></param>
    /// <param name="textSize"></param>
    public static void DrawGlowingTextModern(
      SKCanvas canvas,
      string text,
      float x,
      float y,
      SKColor glowColor,
      SKColor textColor,
      float glowRadius,
      float textSize)
    {
      // Define a common SKFont object for both drawing operations
      using var font = new SKFont();
      font.Size = textSize; // Use the SKFont.Size property

      // 1. Define the glow paint (blurred background)
      using (var glowPaint = new SKPaint())
      {
        glowPaint.IsAntialias = true;
        glowPaint.Style = SKPaintStyle.Fill;
        glowPaint.Color = glowColor;
        // Apply a blur mask filter to create the glow effect
        glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, glowRadius);

        // Draw the blurred text using the new DrawText signature
        // Note: In this specific use case (MaskFilter applied), we still rely on the paint's color.
        canvas.DrawText(text, x, y, font, glowPaint);
      }

      // 2. Define the main text paint (foreground)
      using var textPaint = new SKPaint();
      textPaint.IsAntialias = true;
      textPaint.Style = SKPaintStyle.Fill;
      textPaint.Color = textColor;

      // Draw the sharp text on top using the new DrawText signature
      canvas.DrawText(text, x, y, font, textPaint);
    }

    public static void DrawGlowingTextLegacy(
      SKCanvas canvas,
      string textToDraw,
      float x,
      float y,
      float textSize)
    {
      using var font = new SKFont();
      font.Size = textSize;

      using SKPaint paint = new SKPaint();
      paint.IsAntialias = true;
      paint.Color = SKColors.Blue; // Color of the sharp text foreground

      // Use the modern CreateDropShadow method without the 'shadowMode' enum
      paint.ImageFilter = SKImageFilter.CreateDropShadow(
          dx: 0,
          dy: 0,
          sigmaX: 10, // Adjust sigmaX and sigmaY for the glow size
          sigmaY: 10,
          color: SKColors.Cyan // The glow color
          /* shadowMode parameter is removed */);

      // The resulting image filter automatically draws both the shadow and the foreground
      canvas.DrawText(textToDraw, x, y, font, paint);
    }
  }
}
