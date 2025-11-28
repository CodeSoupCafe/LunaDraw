namespace LunaDraw.Logic.Extensions
{

  using System;

  using SkiaSharp;
  using SkiaSharp.Views.Maui;

  public static class LibraryExtensions
  {
    public enum PaintType
    {
      BasePaint,
      ClearPaint,
      FillPaint,
      HighlightPaint,
      RegionPaint
    }

    public static SKPaint BackgroundPaint => new SKPaint
    {
      Style = SKPaintStyle.Fill,
      Color = SKColors.Black.WithAlpha(50),
    };

    public static SKPaint BasePaint => new SKPaint
    {
      Style = SKPaintStyle.Stroke,
      Color = SKColors.Black,
      StrokeWidth = 3,
      StrokeCap = SKStrokeCap.Round,
      StrokeJoin = SKStrokeJoin.Round,
      IsAntialias = true
    };

    public static SKPaint ClearPaint => new SKPaint
    {
      Style = SKPaintStyle.Stroke,
      BlendMode = SKBlendMode.DstOut,
      //MaskFilter = SKMaskFilter.CreateClip(255, 255),
      Color = SKColors.Red,
      StrokeWidth = 15,
      StrokeCap = SKStrokeCap.Round,
      StrokeJoin = SKStrokeJoin.Round
    };

    public static SKPaint FillPaint => new SKPaint
    {
      Style = SKPaintStyle.Stroke,
      Color = SKColors.Black,
      StrokeWidth = 15,
      StrokeCap = SKStrokeCap.Round,
      StrokeJoin = SKStrokeJoin.Round
    };

    public static SKPaint HighlightPaint => new SKPaint
    {
      PathEffect = SKPathEffect.CreateDash(new[] { 20f, 30f }, 30),
      Style = SKPaintStyle.Stroke,
      Color = SKColors.Blue,
      StrokeWidth = 25,
      StrokeCap = SKStrokeCap.Round,
      StrokeJoin = SKStrokeJoin.Round,
      IsAntialias = true
    };

    public static SKPaint RegionPaint => new SKPaint
    {
      PathEffect = SKPathEffect.CreateDash(new[] { 10f, 20f }, 30),
      Style = SKPaintStyle.Stroke,
      Color = SKColors.White,
      StrokeWidth = 5,
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

    public static Action<object, SKPaintSurfaceEventArgs> DrawShadowText(
        string textToDraw,
        Color? textColor = null,
        int textSize = 0,
        bool centerText = true,
        Color? blurTextColor = null,
        byte horizontalBlurSize = 0x50,
        float overallBlurSize = 15,
        float blurSkewX = 0)
    {
      void InvalidateSurface(object sender, SKPaintSurfaceEventArgs args)
      {
        SKImageInfo info = args.Info;
        SKSurface surface = args.Surface;
        SKCanvas canvas = surface.Canvas;
        float xText = 0, yText = 0;

        canvas.Clear();

        using SKPaint paint = new SKPaint();
        // Set text color
        paint.Color = textColor?.ToSKColor() ?? SKColors.WhiteSmoke;

        if (textSize == 0)
        {
          // Set text size to fill 90% of width
          float width = paint.MeasureText(textToDraw);
          float scale = 0.9f * info.Width / width;
          paint.TextSize *= scale;
        }
        else
        {
          paint.TextSize = textSize;
        }

        // Get text bounds
        SKRect textBounds = new SKRect();
        paint.MeasureText(textToDraw, ref textBounds);

        if (centerText)
        {
          xText = info.Width / 2 - textBounds.MidX;
        }

        // Calculate offsets to position text above center
        yText = info.Height / 4 * 3;

        // Draw unreflected text
        canvas.DrawText(textToDraw, xText, yText, paint);

        // Shift textBounds to match displayed text
        textBounds.Offset(xText, yText);

        var blurTextSKColor = blurTextColor?.ToSKColor() ?? SKColors.WhiteSmoke;

        // Use those offsets to create a gradient for the reflected text
        paint.Shader = SKShader.CreateLinearGradient(
                            new SKPoint(0, textBounds.Top),
                            new SKPoint(0, textBounds.Bottom),
                            new SKColor[] {
                                blurTextSKColor.WithAlpha(0),
                                blurTextSKColor.WithAlpha(horizontalBlurSize)
                            },
                            null,
                            SKShaderTileMode.Clamp);

        // Create a blur mask filter
        paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, overallBlurSize);
        paint.TextSkewX = blurSkewX;

        // Scale the canvas to flip upside-down around the vertical center
        canvas.Scale(1, -0.9f, 0, yText);

        // Draw reflected text
        canvas.DrawText(textToDraw, xText, yText, paint);
      }
      ;

      return InvalidateSurface;
    }
  }
}