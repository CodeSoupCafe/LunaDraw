using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace LunaDraw.Logic.Models
{
  /// <summary>
  /// Represents a group of drawable elements that can be manipulated as a single unit.
  /// </summary>
  public class DrawableGroup : IDrawableElement
  {
    public Guid Id { get; } = Guid.NewGuid();
    public List<IDrawableElement> Children { get; } = [];
    public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

    public bool IsVisible { get; set; } = true;
    private bool isSelected;
    public bool IsSelected
    {
      get => isSelected;
      set
      {
        if (isSelected == value) return;
        isSelected = value;
        foreach (var child in Children)
        {
          child.IsSelected = value;
        }
      }
    }
    public int ZIndex { get; set; }
    public byte Opacity { get; set; } = 255;
    public SKColor? FillColor { get; set; } // Not directly used
    public SKColor StrokeColor { get; set; } // Not directly used
    public float StrokeWidth { get; set; } // Not directly used

    public bool IsGlowEnabled { get; set; } = false;
    public SKColor GlowColor { get; set; } = SKColors.Transparent;
    public float GlowRadius { get; set; } = 0f;

    public SKRect Bounds
    {
      get
      {
        if (!Children.Any()) return SKRect.Empty;

        var left = Children.Min(c => c.Bounds.Left);
        var top = Children.Min(c => c.Bounds.Top);
        var right = Children.Max(c => c.Bounds.Right);
        var bottom = Children.Max(c => c.Bounds.Bottom);

        return new SKRect(left, top, right, bottom);
      }
    }

    public void Draw(SKCanvas canvas)
    {
      if (!IsVisible) return;

      // Check if isolation is needed (if any child uses Clear blend mode)
      var needsIsolation = Children.OfType<DrawablePath>().Any(dp => dp.BlendMode == SKBlendMode.Clear);

      if (needsIsolation)
      {
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha(Opacity) };
        canvas.SaveLayer(paint);
      }

      // The group's transform is applied to children, not to the canvas here
      foreach (var child in Children)
      {
        child.Draw(canvas);
      }

      if (needsIsolation)
      {
        canvas.Restore();
      }
    }

    public bool HitTest(SKPoint point)
    {
      return Children.Any(child => child.HitTest(point));
    }

    public IDrawableElement Clone()
    {
      var newGroup = new DrawableGroup
      {
        TransformMatrix = TransformMatrix,
        IsVisible = IsVisible,
        IsSelected = false,
        ZIndex = ZIndex,
        Opacity = Opacity,
        IsGlowEnabled = IsGlowEnabled,
        GlowColor = GlowColor,
        GlowRadius = GlowRadius
      };
      foreach (var child in Children)
      {
        newGroup.Children.Add(child.Clone());
      }
      return newGroup;
    }

    public void Translate(SKPoint offset)
    {
      var matrix = SKMatrix.CreateTranslation(offset.X, offset.Y);
      Transform(matrix);
    }

    public void Transform(SKMatrix matrix)
    {
      // Apply the transformation to all children
      foreach (var child in Children)
      {
        child.Transform(matrix);
      }
    }

    public SKPath GetPath()
    {
      var path = new SKPath();
      foreach (var child in Children)
      {
        using var childPath = child.GetPath();
        if (child is DrawablePath dp && dp.BlendMode == SKBlendMode.Clear)
        {
          var result = new SKPath();
          if (path.Op(childPath, SKPathOp.Difference, result))
          {
            path.Dispose();
            path = result;
          }
        }
        else
        {
          var result = new SKPath();
          if (path.Op(childPath, SKPathOp.Union, result))
          {
            path.Dispose();
            path = result;
          }
        }
      }
      return path;
    }

    public SKPath GetGeometryPath()
    {
        var path = new SKPath();
        foreach (var child in Children)
        {
            using var childPath = child.GetGeometryPath();
            // Union all child geometry paths
            var result = new SKPath();
            if (path.Op(childPath, SKPathOp.Union, result))
            {
                path.Dispose();
                path = result;
            }
        }
        return path;
    }
  }
}