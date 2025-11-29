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

    private bool _isRainbowEnabled;
    public bool IsRainbowEnabled
    {
        get => _isRainbowEnabled;
        set
        {
            if (_isRainbowEnabled != value)
            {
                _isRainbowEnabled = value;
                InvalidateCache();
            }
        }
    }

    private float _sizeJitter;
    public float SizeJitter
    {
        get => _sizeJitter;
        set
        {
            if (Math.Abs(_sizeJitter - value) > 0.001f)
            {
                _sizeJitter = value;
                InvalidateCache();
            }
        }
    }

    private float _angleJitter;
    public float AngleJitter
    {
        get => _angleJitter;
        set
        {
            if (Math.Abs(_angleJitter - value) > 0.001f)
            {
                _angleJitter = value;
                InvalidateCache();
            }
        }
    }

    private float _hueJitter;
    public float HueJitter
    {
        get => _hueJitter;
        set
        {
            if (Math.Abs(_hueJitter - value) > 0.001f)
            {
                _hueJitter = value;
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
        
        // Conservative bounds with jitter
        float maxScale = 1.0f + SizeJitter; // Assuming SizeJitter is 0-1 relative addition
        float halfSize = Size * maxScale;
        
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

        float baseScale = Size / 20f;
        using var scaledPath = new SKPath(Shape.Path);
        var scaleMatrix = SKMatrix.CreateScale(baseScale, baseScale);
        scaledPath.Transform(scaleMatrix);

        using var sharedPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = BlendMode
        };

        // Pre-calculate or deterministically generate variations
        // Using a simple random generator seeded with a constant for stability if needed,
        // but here we might just use index-based hashing for stateless drawing.

        // Glow pass (Optimized with SaveLayer)
        if (IsGlowEnabled && GlowRadius > 0)
        {
            using var glowLayerPaint = new SKPaint
            {
                ImageFilter = SKImageFilter.CreateBlur(GlowRadius, GlowRadius),
                IsAntialias = true
            };
            
            canvas.SaveLayer(glowLayerPaint);

            int index = 0;
            foreach (var point in Points)
            {
                DrawSingleStamp(canvas, scaledPath, point, index, true, sharedPaint);
                index++;
            }
            
            canvas.Restore(); // Apply blur
        }

        // Main pass
        int i = 0;
        foreach (var point in Points)
        {
            DrawSingleStamp(canvas, scaledPath, point, i, false, sharedPaint);
            i++;
        }
    }

    private void DrawSingleStamp(SKCanvas canvas, SKPath basePath, SKPoint point, int index, bool isGlowPass, SKPaint paint)
    {
        // Deterministic Random based on index
        var random = new Random(index * 1337); // Simple seed

        // Size Jitter
        float scaleFactor = 1.0f;
        if (SizeJitter > 0)
        {
            float jitter = (float)random.NextDouble() * SizeJitter; // 0 to SizeJitter
            scaleFactor = 1.0f + ((float)random.NextDouble() - 0.5f) * 2.0f * SizeJitter;
            if (scaleFactor < 0.1f) scaleFactor = 0.1f;
        }

        // Angle Jitter
        float rotation = 0f;
        if (AngleJitter > 0)
        {
            rotation = ((float)random.NextDouble() - 0.5f) * 2.0f * AngleJitter; // +/- AngleJitter
        }

        // Color Jitter / Rainbow
        SKColor color = isGlowPass ? GlowColor : StrokeColor;
        
        if (IsRainbowEnabled)
        {
            // Rainbow cycles through Hue based on index
            float hue = (index * 10) % 360; // 10 degrees per stamp
            color = SKColor.FromHsl(hue, 100, 50);
        }
        else if (HueJitter > 0 && !isGlowPass)
        {
            // Apply Hue Jitter
            color.ToHsl(out float h, out float s, out float l);
            float jitter = ((float)random.NextDouble() - 0.5f) * 2.0f * HueJitter * 360f; // +/- HueJitter (0-1 -> 0-360)
            h = (h + jitter) % 360f;
            if (h < 0) h += 360f;
            color = SKColor.FromHsl(h, s, l);
        }

        // Apply opacity and flow
        paint.Color = color.WithAlpha((byte)(Flow * (Opacity / 255f)));

        canvas.Save();
        canvas.Translate(point.X, point.Y);
        
        if (rotation != 0)
        {
            canvas.RotateDegrees(rotation);
        }
        
        if (scaleFactor != 1.0f)
        {
            canvas.Scale(scaleFactor);
        }

        canvas.DrawPath(basePath, paint);
        canvas.Restore();
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
        GlowRadius = GlowRadius,
        IsRainbowEnabled = IsRainbowEnabled,
        SizeJitter = SizeJitter,
        AngleJitter = AngleJitter,
        HueJitter = HueJitter
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