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

namespace LunaDraw.Logic.Models;

/// <summary>
/// Represents a freehand drawn path on the canvas.
/// </summary>
public class DrawablePath : IDrawableElement
{
  public Guid Id { get; init; } = Guid.NewGuid();
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
  public required SKPath Path { get; set; }
  public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

  public bool IsVisible { get; set; } = true;
  public bool IsSelected { get; set; }
  public int ZIndex { get; set; }
  public byte Opacity { get; set; } = 255;
  public SKColor? FillColor { get; set; }
  public SKColor StrokeColor { get; set; }
  public float StrokeWidth { get; set; }
  public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;
  public bool IsFilled { get; set; }
  public SKShader? FillShader { get; set; }

  public bool IsGlowEnabled { get; set; } = false;
  public SKColor GlowColor { get; set; } = SKColors.Transparent;
  public float GlowRadius { get; set; } = 0f;
  public float AnimationProgress { get; set; } = 1.0f;

  public SKRect Bounds => TransformMatrix.MapRect(Path?.TightBounds ?? SKRect.Empty);

  public void Draw(SKCanvas canvas)
  {
    if (!IsVisible || Path == null) return;
    if (AnimationProgress <= 0f) return;

    SKPath? path = Path;
    bool isPartial = AnimationProgress < 1.0f;

    if (isPartial)
    {
      path = new SKPath();
      using var measure = new SKPathMeasure(Path, false, 1.0f);
      float length = measure.Length * AnimationProgress;
      measure.GetSegment(0, length, path, true);
    }

    try 
    {
        canvas.Save();
        var matrix = TransformMatrix;
        canvas.Concat(in matrix);

        if (IsGlowEnabled && GlowRadius > 0)
        {
          using var glowPaint = new SKPaint
          {
            Style = IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
            Color = GlowColor.WithAlpha(Opacity),
            StrokeWidth = StrokeWidth,
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, GlowRadius)
          };
          canvas.DrawPath(path, glowPaint);
        }

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
          canvas.DrawPath(path, highlightPaint);
        }

        // Draw Fill
        if (IsFilled)
        {
          using var fillPaint = new SKPaint
          {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = BlendMode
          };

          if (FillShader != null)
          {
            fillPaint.Shader = FillShader;
            // Modulate with opacity/color if needed, but usually white for image shaders
            fillPaint.Color = SKColors.White.WithAlpha(Opacity);
          }
          else if (FillColor.HasValue)
          {
            fillPaint.Color = FillColor.Value.WithAlpha(Opacity);
          }
          else
          {
            // Fallback: use StrokeColor as fill if no FillColor (matching legacy behavior)
            fillPaint.Color = StrokeColor.WithAlpha(Opacity);
          }
          canvas.DrawPath(path, fillPaint);
        }

        // Draw Stroke
        if (StrokeWidth > 0)
        {
          // Only draw stroke if it's an outline (not filled) OR if it has an explicit fill color (so we preserve border)
          // If it is filled but has NO fill color, it is a "solid blob" using StrokeColor, so we skip stroking to avoid double-draw/expansion
          // BUT if we have a FillShader, we definitely want the stroke if it exists.
          bool shouldStroke = !IsFilled || (IsFilled && (FillColor.HasValue || FillShader != null));

          if (shouldStroke)
          {
            using var strokePaint = new SKPaint
            {
              Style = SKPaintStyle.Stroke,
              Color = StrokeColor.WithAlpha(Opacity),
              StrokeWidth = StrokeWidth,
              IsAntialias = true,
              BlendMode = BlendMode,
              StrokeCap = SKStrokeCap.Round,
              StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(path, strokePaint);
          }
        }
    }
    finally
    {
        canvas.Restore();
        if (isPartial)
        {
            path?.Dispose();
        }
    }
  }

  public bool HitTest(SKPoint point)
  {
    if (Path == null) return false;

    if (!TransformMatrix.TryInvert(out var inverseMatrix))
      return false;

    var localPoint = inverseMatrix.MapPoint(point);

    // Check if point is within bounds first (faster)
    if (!Path.TightBounds.Contains(localPoint)) return false;

    // Check if path contains point with tolerance
    if (IsFilled)
    {
      return Path.Contains(localPoint.X, localPoint.Y);
    }
    else
    {
      using var paint = new SKPaint
      {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = StrokeWidth + 5 // Add tolerance
      };
      using var strokedPath = new SKPath();
      paint.GetFillPath(Path, strokedPath);
      return strokedPath.Contains(localPoint.X, localPoint.Y);
    }
  }

  public IDrawableElement Clone()
  {
    return new DrawablePath
    {
      Path = new SKPath(Path),
      TransformMatrix = TransformMatrix,
      IsVisible = IsVisible,
      IsSelected = false,
      ZIndex = ZIndex,
      Opacity = Opacity,
      FillColor = FillColor,
      StrokeColor = StrokeColor,
      StrokeWidth = StrokeWidth,
      BlendMode = BlendMode,
      IsFilled = IsFilled,
      FillShader = FillShader // Share shader reference
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
    var path = new SKPath(Path);

    if (!IsFilled && StrokeWidth > 0)
    {
      using var paint = new SKPaint
      {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = StrokeWidth,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round
      };
      var strokePath = new SKPath();
      paint.GetFillPath(path, strokePath);
      path.Dispose();
      path = strokePath;
    }
    // If IsFilled is true, we assume the path itself is the shape.
    // If it has a stroke AND fill, we should technically union them, 
    // but for freehand paths, usually it's either stroke or fill.
    // If we support both later, we can add the union logic here.

    path.Transform(TransformMatrix);
    return path;
  }

  public SKPath GetGeometryPath()
  {
    var path = new SKPath(Path);
    path.Transform(TransformMatrix);
    return path;
  }
}
