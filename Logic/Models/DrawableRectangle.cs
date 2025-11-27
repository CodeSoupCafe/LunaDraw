using SkiaSharp;

namespace LunaDraw.Logic.Models
{
  /// <summary>
  /// Represents a rectangle shape on the canvas.
  /// </summary>
  public class DrawableRectangle : IDrawableElement
  {
    public Guid Id { get; } = Guid.NewGuid();
    public SKRect Rectangle { get; set; }
    public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

    public bool IsVisible { get; set; } = true;
    public bool IsSelected { get; set; }
    public int ZIndex { get; set; }
    public byte Opacity { get; set; } = 255;
    public SKColor? FillColor { get; set; }
    public SKColor StrokeColor { get; set; }
    public float StrokeWidth { get; set; }

    public SKRect Bounds => TransformMatrix.MapRect(Rectangle);

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
        canvas.DrawRect(Rectangle, highlightPaint);
      }

      // Draw fill if specified
      if (FillColor.HasValue)
      {
        using var fillPaint = new SKPaint
        {
          Style = SKPaintStyle.Fill,
          Color = FillColor.Value.WithAlpha(Opacity),
          IsAntialias = true
        };
        canvas.DrawRect(Rectangle, fillPaint);
      }

      // Draw stroke
      using var strokePaint = new SKPaint
      {
        Style = SKPaintStyle.Stroke,
        Color = StrokeColor.WithAlpha(Opacity),
        StrokeWidth = StrokeWidth,
        IsAntialias = true
      };
      canvas.DrawRect(Rectangle, strokePaint);

      canvas.Restore();
    }

    public bool HitTest(SKPoint point)
    {
      if (!TransformMatrix.TryInvert(out var inverseMatrix))
        return false;

      var localPoint = inverseMatrix.MapPoint(point);

      using var path = new SKPath();
      path.AddRect(Rectangle);

      // Check if filled and point is inside the fill path
      if (FillColor.HasValue && path.Contains(localPoint.X, localPoint.Y))
      {
        return true;
      }

      // Check if point is near the stroke
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
      return new DrawableRectangle
      {
        Rectangle = Rectangle,
        TransformMatrix = TransformMatrix,
        IsVisible = IsVisible,
        IsSelected = false,
        ZIndex = ZIndex,
        Opacity = Opacity,
        FillColor = FillColor,
        StrokeColor = StrokeColor,
        StrokeWidth = StrokeWidth
      };
    }

    public void Translate(SKPoint offset)
    {
      var translation = SKMatrix.CreateTranslation(offset.X, offset.Y);
      TransformMatrix = SKMatrix.Concat(TransformMatrix, translation);
    }

    public void Transform(SKMatrix matrix)
    {
      TransformMatrix = SKMatrix.Concat(matrix, TransformMatrix);
    }

    public SKPath GetPath()
    {
      var path = new SKPath();
      path.AddRect(Rectangle);

      if (StrokeWidth > 0)
      {
        using var paint = new SKPaint
        {
          Style = SKPaintStyle.Stroke,
          StrokeWidth = StrokeWidth,
          StrokeJoin = SKStrokeJoin.Miter
        };
        var strokePath = new SKPath();
        paint.GetFillPath(path, strokePath);

        if (FillColor.HasValue)
        {
          var combined = new SKPath();
          // Union the fill (original path) and the stroke
          // path OP strokePath -> combined
          path.Op(strokePath, SKPathOp.Union, combined);
          path.Dispose();
          path = combined;
        }
        else
        {
          path.Dispose();
          path = strokePath;
        }
      }

      path.Transform(TransformMatrix);
      return path;
    }
  }
}
