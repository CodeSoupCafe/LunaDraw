using SkiaSharp;

namespace LunaDraw.Components.Carousel;

public static class LibraryExtensions
{

  public static SKPaint BasePaint => new SKPaint
  {
    Style = SKPaintStyle.Stroke,
    Color = SKColors.Black,
    StrokeWidth = 3,
    StrokeCap = SKStrokeCap.Round,
    StrokeJoin = SKStrokeJoin.Round,
    IsAntialias = true
  };

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