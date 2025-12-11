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

namespace LunaDraw.Logic.Models
{
  /// <summary>
  /// Represents a line shape on the canvas.
  /// </summary>
  public class DrawableLine : IDrawableElement
  {
    public Guid Id { get; } = Guid.NewGuid();
    public SKPoint StartPoint { get; set; }
    public SKPoint EndPoint { get; set; }
    public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

    public bool IsVisible { get; set; } = true;
    public bool IsSelected { get; set; }
    public int ZIndex { get; set; }
    public byte Opacity { get; set; } = 255;
    public SKColor? FillColor { get; set; } // Not used for line
    public SKColor StrokeColor { get; set; }
    public float StrokeWidth { get; set; }
    public bool IsGlowEnabled { get; set; } = false;
    public SKColor GlowColor { get; set; } = SKColors.Transparent;
    public float GlowRadius { get; set; } = 0f;

    public SKRect Bounds
    {
      get
      {
        var localBounds = new SKRect(
            Math.Min(StartPoint.X, EndPoint.X),
            Math.Min(StartPoint.Y, EndPoint.Y),
            Math.Max(StartPoint.X, EndPoint.X),
            Math.Max(StartPoint.Y, EndPoint.Y)
        );
        return TransformMatrix.MapRect(localBounds);
      }
    }

    public void Draw(SKCanvas canvas)
    {
      if (!IsVisible) return;

      canvas.Save();
      var matrix = TransformMatrix;
      canvas.Concat(in matrix);

      // Draw selection highlight
      if (IsSelected)
      {
        using var highlightPaint = new SKPaint
        {
          Style = SKPaintStyle.Stroke,
          Color = SKColors.DodgerBlue.WithAlpha(128),
          StrokeWidth = StrokeWidth + 4,
          IsAntialias = true
        };
        canvas.DrawLine(StartPoint, EndPoint, highlightPaint);
      }

      // Draw glow if enabled
      if (IsGlowEnabled && GlowRadius > 0)
      {
        using var glowPaint = new SKPaint
        {
          Style = SKPaintStyle.Stroke,
          Color = GlowColor.WithAlpha(Opacity),
          StrokeWidth = StrokeWidth,
          IsAntialias = true,
          MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, GlowRadius)
        };
        canvas.DrawLine(StartPoint, EndPoint, glowPaint);
      }

      using var paint = new SKPaint
      {
        Style = SKPaintStyle.Stroke,
        Color = StrokeColor.WithAlpha(Opacity),
        StrokeWidth = StrokeWidth,
        IsAntialias = true
      };
      canvas.DrawLine(StartPoint, EndPoint, paint);

      canvas.Restore();
    }

    public bool HitTest(SKPoint point)
    {
      if (!TransformMatrix.TryInvert(out var inverseMatrix))
        return false;

      var localPoint = inverseMatrix.MapPoint(point);

      // Use path-based hit testing for accuracy in local space
      using var path = new SKPath();
      path.MoveTo(StartPoint);
      path.LineTo(EndPoint);

      using var paint = new SKPaint
      {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = StrokeWidth + 10 // Add tolerance
      };
      using var strokedPath = new SKPath();
      paint.GetFillPath(path, strokedPath);
      return strokedPath.Contains(localPoint.X, localPoint.Y);
    }

    public IDrawableElement Clone()
    {
      return new DrawableLine
      {
        StartPoint = StartPoint,
        EndPoint = EndPoint,
        TransformMatrix = TransformMatrix,
        IsVisible = IsVisible,
        IsSelected = false,
        ZIndex = ZIndex,
        Opacity = Opacity,
        StrokeColor = StrokeColor,
        StrokeWidth = StrokeWidth,
        IsGlowEnabled = IsGlowEnabled,
        GlowColor = GlowColor,
        GlowRadius = GlowRadius
      };
    }

    public void Translate(SKPoint offset)
    {
      var translation = SKMatrix.CreateTranslation(offset.X, offset.Y);
      TransformMatrix = SKMatrix.Concat(translation, TransformMatrix);
    }

    public void Transform(SKMatrix matrix)
    {
      TransformMatrix = SKMatrix.Concat(matrix, TransformMatrix);
    }

    public SKPath GetPath()
    {
      var path = new SKPath();
      path.MoveTo(StartPoint);
      path.LineTo(EndPoint);

      // Lines are always stroked (no fill)
      if (StrokeWidth > 0)
      {
        using var paint = new SKPaint
        {
          Style = SKPaintStyle.Stroke,
          StrokeWidth = StrokeWidth,
          StrokeCap = SKStrokeCap.Round
        };
        var strokePath = new SKPath();
        paint.GetFillPath(path, strokePath);
        path.Dispose();
        path = strokePath;
      }

      path.Transform(TransformMatrix);
      return path;
    }

    public SKPath GetGeometryPath()
    {
        var path = new SKPath();
        path.MoveTo(StartPoint);
        path.LineTo(EndPoint);
        path.Transform(TransformMatrix);
        return path;
    }
  }
}
