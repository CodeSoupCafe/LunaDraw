using SkiaSharp;

namespace LunaDraw.Logic.Models
{
  /// <summary>
  /// Represents a series of stamped shapes (custom brush strokes).
  /// </summary>
  public class DrawableStamps : IDrawableElement
  {
    private SKBitmap? _cachedBitmap;
    private SKPoint _cacheOffset;
    private bool _isCacheDirty = true;

    public Guid Id { get; } = Guid.NewGuid();

    private List<SKPoint> _points = [];
    public List<SKPoint> Points
    {
        get => _points;
        set
        {
            _points = value;
            InvalidateCache();
        }
    }

    private BrushShape _shape = BrushShape.Circle();
    public BrushShape Shape
    {
        get => _shape;
        set
        {
            _shape = value;
            InvalidateCache();
        }
    }

    private float _size = 10f;
    public float Size
    {
        get => _size;
        set
        {
            if (Math.Abs(_size - value) > 0.001f)
            {
                _size = value;
                InvalidateCache();
            }
        }
    }

    private byte _flow = 255;
    public byte Flow
    {
        get => _flow;
        set
        {
            if (_flow != value)
            {
                _flow = value;
                InvalidateCache();
            }
        }
    }

    public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    public bool IsSelected { get; set; }
    public int ZIndex { get; set; }

    private byte _opacity = 255;
    public byte Opacity
    {
        get => _opacity;
        set
        {
            if (_opacity != value)
            {
                _opacity = value;
                InvalidateCache();
            }
        }
    }

    public SKColor? FillColor { get; set; }

    private SKColor _strokeColor = SKColors.Black;
    public SKColor StrokeColor
    {
        get => _strokeColor;
        set
        {
            if (_strokeColor != value)
            {
                _strokeColor = value;
                InvalidateCache();
            }
        }
    }

    public float StrokeWidth { get; set; } // Not used directly, using Size instead

    private SKBlendMode _blendMode = SKBlendMode.SrcOver;
    public SKBlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (_blendMode != value)
            {
                _blendMode = value;
                InvalidateCache();
            }
        }
    }

    public bool IsFilled { get; set; } = true;

    private bool _isGlowEnabled = false;
    public bool IsGlowEnabled
    {
        get => _isGlowEnabled;
        set
        {
            if (_isGlowEnabled != value)
            {
                _isGlowEnabled = value;
                InvalidateCache();
            }
        }
    }

    private SKColor _glowColor = SKColors.Transparent;
    public SKColor GlowColor
    {
        get => _glowColor;
        set
        {
            if (_glowColor != value)
            {
                _glowColor = value;
                InvalidateCache();
            }
        }
    }

    private float _glowRadius = 0f;
    public float GlowRadius
    {
        get => _glowRadius;
        set
        {
            if (Math.Abs(_glowRadius - value) > 0.001f)
            {
                _glowRadius = value;
                InvalidateCache();
            }
        }
    }

    private void InvalidateCache()
    {
        _isCacheDirty = true;
        _cachedBitmap?.Dispose();
        _cachedBitmap = null;
    }

    private SKRect GetLocalBounds()
    {
        if (Points == null || !Points.Any()) return SKRect.Empty;
        float halfSize = Size;
        float minX = Points.Min(p => p.X);
        float minY = Points.Min(p => p.Y);
        float maxX = Points.Max(p => p.X);
        float maxY = Points.Max(p => p.Y);
        
        float glowPadding = IsGlowEnabled ? GlowRadius * 3 : 0;
        float padding = glowPadding + 5; // Extra safety margin

        return new SKRect(
            minX - halfSize - padding, 
            minY - halfSize - padding, 
            maxX + halfSize + padding, 
            maxY + halfSize + padding);
    }

    public SKRect Bounds => TransformMatrix.MapRect(GetLocalBounds());

    private void UpdateCache()
    {
        if (!_isCacheDirty && _cachedBitmap != null) return;
        if (Points == null || !Points.Any() || Shape?.Path == null) return;

        _cachedBitmap?.Dispose();
        _cachedBitmap = null;

        var bounds = GetLocalBounds();
        var width = (int)Math.Ceiling(bounds.Width);
        var height = (int)Math.Ceiling(bounds.Height);

        if (width <= 0 || height <= 0) return;

        _cachedBitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(_cachedBitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(-bounds.Left, -bounds.Top);

        DrawContent(canvas);

        _cacheOffset = new SKPoint(bounds.Left, bounds.Top);
        _isCacheDirty = false;
    }

    private void DrawContent(SKCanvas canvas)
    {
        if (Points == null || !Points.Any() || Shape?.Path == null) return;

        float scale = Size / 20f;
        using var scaledPath = new SKPath(Shape.Path);
        var scaleMatrix = SKMatrix.CreateScale(scale, scale);
        scaledPath.Transform(scaleMatrix);

        // Glow pass (Optimized with SaveLayer)
        if (IsGlowEnabled && GlowRadius > 0)
        {
            using var glowLayerPaint = new SKPaint
            {
                ImageFilter = SKImageFilter.CreateBlur(GlowRadius, GlowRadius),
                IsAntialias = true
            };
            
            canvas.SaveLayer(glowLayerPaint);

            using var glowContentPaint = new SKPaint
            {
               Style = SKPaintStyle.Fill,
               Color = GlowColor.WithAlpha((byte)(Flow * (Opacity / 255f))),
               IsAntialias = true,
               BlendMode = BlendMode
            };

            foreach (var point in Points)
            {
                canvas.Save();
                canvas.Translate(point.X, point.Y);
                canvas.DrawPath(scaledPath, glowContentPaint);
                canvas.Restore();
            }
            
            canvas.Restore(); // Apply blur
        }

        // Main pass
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = StrokeColor.WithAlpha((byte)(Flow * (Opacity / 255f))),
            IsAntialias = true,
            BlendMode = BlendMode
        };

        foreach (var point in Points)
        {
            canvas.Save();
            canvas.Translate(point.X, point.Y);
            canvas.DrawPath(scaledPath, paint);
            canvas.Restore();
        }
    }

    public void Draw(SKCanvas canvas)
    {
        if (!IsVisible) return;

        canvas.Save();
        var matrix = TransformMatrix;
        canvas.Concat(in matrix);

        if (IsSelected)
        {
            // Draw selection highlight based on simple bounds (ignoring glow for the box)
            float halfSize = Size;
            float minX = Points.Min(p => p.X);
            float minY = Points.Min(p => p.Y);
            float maxX = Points.Max(p => p.X);
            float maxY = Points.Max(p => p.Y);
            var localBounds = new SKRect(minX - halfSize, minY - halfSize, maxX + halfSize, maxY + halfSize);

            using var highlightPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.DodgerBlue.WithAlpha(128),
                StrokeWidth = 2,
                IsAntialias = true
            };
            canvas.DrawRect(localBounds, highlightPaint);
        }

        // Always try to use cache for content
        UpdateCache();
        if (_cachedBitmap != null)
        {
            canvas.DrawBitmap(_cachedBitmap, _cacheOffset);
        }
        else
        {
            // Fallback
            DrawContent(canvas);
        }

        canvas.Restore();
    }

    public bool HitTest(SKPoint point)
    {
      if (!TransformMatrix.TryInvert(out var inverseMatrix))
        return false;

      var localPoint = inverseMatrix.MapPoint(point);

      // Simple bounding box check for now
      float halfSize = Size;
      float minX = Points.Min(p => p.X);
      float minY = Points.Min(p => p.Y);
      float maxX = Points.Max(p => p.X);
      float maxY = Points.Max(p => p.Y);
      var localBounds = new SKRect(minX - halfSize, minY - halfSize, maxX + halfSize, maxY + halfSize);

      return localBounds.Contains(localPoint);
    }

    public IDrawableElement Clone()
    {
      return new DrawableStamps
      {
        Points = new List<SKPoint>(Points),
        Shape = Shape, // Reference copy is fine for shape
        Size = Size,
        Flow = Flow,
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
        IsGlowEnabled = IsGlowEnabled,
        GlowColor = GlowColor,
        GlowRadius = GlowRadius
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
      // Returning a combined path is expensive but necessary if we want to convert to standard path
      var combinedPath = new SKPath();
      float scale = Size / 20f;
      using var scaledPath = new SKPath(Shape.Path);
      var scaleMatrix = SKMatrix.CreateScale(scale, scale);
      scaledPath.Transform(scaleMatrix);

      foreach (var point in Points)
      {
        var p = new SKPath(scaledPath);
        p.Transform(SKMatrix.CreateTranslation(point.X, point.Y));
        combinedPath.AddPath(p);
      }
      combinedPath.Transform(TransformMatrix);
      return combinedPath;
    }
  }
}