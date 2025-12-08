using SkiaSharp;

namespace LunaDraw.Logic.Models
{
  /// <summary>
  /// Represents a series of stamped shapes (custom brush strokes).
  /// </summary>
  public class DrawableStamps : IDrawableElement
  {
    private SKBitmap? cachedBitmap;
    private SKPoint cacheOffset;
    private bool isCacheDirty = true;

    public Guid Id { get; } = Guid.NewGuid();

    private List<SKPoint> points = [];
    public List<SKPoint> Points
    {
        get => points;
        set
        {
            points = value;
            InvalidateCache();
        }
    }

    private BrushShape shape = BrushShape.Circle();
    public BrushShape Shape
    {
        get => shape;
        set
        {
            shape = value;
            InvalidateCache();
        }
    }

    private float size = 10f;
    public float Size
    {
        get => size;
        set
        {
            if (Math.Abs(size - value) > 0.001f)
            {
                size = value;
                InvalidateCache();
            }
        }
    }

    private byte flow = 255;
    public byte Flow
    {
        get => flow;
        set
        {
            if (flow != value)
            {
                flow = value;
                InvalidateCache();
            }
        }
    }

    public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

    private bool isVisible = true;
    public bool IsVisible
    {
        get => isVisible;
        set => isVisible = value;
    }

    public bool IsSelected { get; set; }
    public int ZIndex { get; set; }

    private byte opacity = 255;
    public byte Opacity
    {
        get => opacity;
        set
        {
            if (opacity != value)
            {
                opacity = value;
                InvalidateCache();
            }
        }
    }

    public SKColor? FillColor { get; set; }

    private SKColor strokeColor = SKColors.Black;
    public SKColor StrokeColor
    {
        get => strokeColor;
        set
        {
            if (strokeColor != value)
            {
                strokeColor = value;
                InvalidateCache();
            }
        }
    }

    public float StrokeWidth { get; set; } // Not used directly, using Size instead

    private SKBlendMode blendMode = SKBlendMode.SrcOver;
    public SKBlendMode BlendMode
    {
        get => blendMode;
        set
        {
            if (blendMode != value)
            {
                blendMode = value;
                InvalidateCache();
            }
        }
    }

    public bool IsFilled { get; set; } = true;

    private bool isGlowEnabled = false;
    public bool IsGlowEnabled
    {
        get => isGlowEnabled;
        set
        {
            if (isGlowEnabled != value)
            {
                isGlowEnabled = value;
                InvalidateCache();
            }
        }
    }

    private SKColor glowColor = SKColors.Transparent;
    public SKColor GlowColor
    {
        get => glowColor;
        set
        {
            if (glowColor != value)
            {
                glowColor = value;
                InvalidateCache();
            }
        }
    }

    private float glowRadius = 0f;
    public float GlowRadius
    {
        get => glowRadius;
        set
        {
            if (Math.Abs(glowRadius - value) > 0.001f)
            {
                glowRadius = value;
                InvalidateCache();
            }
        }
    }

    private bool isRainbowEnabled;
    public bool IsRainbowEnabled
    {
        get => isRainbowEnabled;
        set
        {
            if (isRainbowEnabled != value)
            {
                isRainbowEnabled = value;
                InvalidateCache();
            }
        }
    }

    private List<float> rotations = [];
    public List<float> Rotations
    {
        get => rotations;
        set
        {
            rotations = value;
            InvalidateCache();
        }
    }

    private float sizeJitter;
    public float SizeJitter
    {
        get => sizeJitter;
        set
        {
            if (Math.Abs(sizeJitter - value) > 0.001f)
            {
                sizeJitter = value;
                InvalidateCache();
            }
        }
    }

    private float angleJitter;
    public float AngleJitter
    {
        get => angleJitter;
        set
        {
            if (Math.Abs(angleJitter - value) > 0.001f)
            {
                angleJitter = value;
                InvalidateCache();
            }
        }
    }

    private float hueJitter;
    public float HueJitter
    {
        get => hueJitter;
        set
        {
            if (Math.Abs(hueJitter - value) > 0.001f)
            {
                hueJitter = value;
                InvalidateCache();
            }
        }
    }

    private void InvalidateCache()
    {
        isCacheDirty = true;
        cachedBitmap?.Dispose();
        cachedBitmap = null;
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
        if (!isCacheDirty && cachedBitmap != null) return;
        if (Points == null || !Points.Any() || Shape?.Path == null) return;

        cachedBitmap?.Dispose();
        cachedBitmap = null;

        var bounds = GetLocalBounds();
        var width = (int)Math.Ceiling(bounds.Width);
        var height = (int)Math.Ceiling(bounds.Height);

        if (width <= 0 || height <= 0) return;

        cachedBitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(cachedBitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(-bounds.Left, -bounds.Top);

        DrawContent(canvas);

        cacheOffset = new SKPoint(bounds.Left, bounds.Top);
        isCacheDirty = false;
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
        float rotationDelta = 0f;
        if (AngleJitter > 0)
        {
            rotationDelta = ((float)random.NextDouble() - 0.5f) * 2.0f * AngleJitter; // +/- AngleJitter
        }

        // Color Jitter / Rainbow
        SKColor color = isGlowPass ? GlowColor : StrokeColor;
        
        if (IsRainbowEnabled)
        {
            // Rainbow cycles through Hue based on index
            float hue = index * 10 % 360; // 10 degrees per stamp
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
        
        // Apply Base Rotation (from path direction) + Jitter
        float baseRotation = (Rotations != null && index < Rotations.Count) ? Rotations[index] : 0f;
        float finalRotation = baseRotation + rotationDelta;

        if (Math.Abs(finalRotation) > 0.001f)
        {
            canvas.RotateDegrees(finalRotation);
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
        if (cachedBitmap != null)
        {
            canvas.DrawBitmap(cachedBitmap, cacheOffset);
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
      if (Points == null || !Points.Any() || Shape?.Path == null) return false;

      if (!TransformMatrix.TryInvert(out var inverseMatrix))
        return false;

      var localPoint = inverseMatrix.MapPoint(point);

      // We need to iterate through each stamp and perform a hit test on its individual path
      // replicating the jitter and transformations
      float baseScale = Size / 20f;
      using var scaledPath = new SKPath(Shape.Path);
      var initialScaleMatrix = SKMatrix.CreateScale(baseScale, baseScale);
      scaledPath.Transform(initialScaleMatrix); // Apply base scale once

      for (int index = 0; index < Points.Count; index++)
      {
        var stampPoint = Points[index];
        // Deterministic Random based on index
        var random = new Random(index * 1337);

        // Calculate current stamp's transformations
        float currentScaleFactor = 1.0f;
        if (SizeJitter > 0)
        {
            float unusedJitter = (float)random.NextDouble() * SizeJitter; // Match DrawSingleStamp consumption
            currentScaleFactor = 1.0f + ((float)random.NextDouble() - 0.5f) * 2.0f * SizeJitter;
            if (currentScaleFactor < 0.1f) currentScaleFactor = 0.1f;
        }
        
        float currentRotationDelta = 0f;
        if (AngleJitter > 0)
        {
            currentRotationDelta = ((float)random.NextDouble() - 0.5f) * 2.0f * AngleJitter;
        }
        float baseRotation = (Rotations != null && index < Rotations.Count) ? Rotations[index] : 0f;
        float finalRotation = baseRotation + currentRotationDelta;

        // Create the individual stamp's path with its transformations
        using var stampPath = new SKPath(scaledPath); // Start with the pre-scaled shape path
        
        // Apply individual stamp's jittered scale and rotation
        SKMatrix stampTransform = SKMatrix.CreateScale(currentScaleFactor, currentScaleFactor, 0, 0);
        stampTransform = stampTransform.PostConcat(SKMatrix.CreateRotationDegrees(finalRotation, 0, 0));
        stampPath.Transform(stampTransform);

        // Translate to stamp's center point
        stampPath.Transform(SKMatrix.CreateTranslation(stampPoint.X, stampPoint.Y));

        // Check for visible fill hit (Alpha > 0)
        SKColor effectiveFillColor = StrokeColor; // Stamps are usually filled with stroke color for simplicity
        
        // If stamp opacity * element opacity makes it transparent, it shouldn't hit.
        // For stamps, Flow * Opacity is the effective alpha applied to color.
        byte effectiveAlpha = (byte)(Flow * (Opacity / 255f));

        if (effectiveAlpha > 0 && stampPath.Contains(localPoint.X, localPoint.Y))
        {
            return true;
        }
      }
      
      return false; // No stamp hit
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
        Rotations = new List<float>(Rotations),
        SizeJitter = SizeJitter,
        AngleJitter = AngleJitter,
        HueJitter = HueJitter
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
      // Returning a combined path is expensive but necessary if we want to convert to standard path
      var combinedPath = new SKPath();
      float baseScale = Size / 20f;
      using var scaledPath = new SKPath(Shape.Path);
      var scaleMatrix = SKMatrix.CreateScale(baseScale, baseScale);
      scaledPath.Transform(scaleMatrix);

      for (int i = 0; i < Points.Count; i++)
      {
        var point = Points[i];
        var random = new Random(i * 1337);

        float currentScaleFactor = 1.0f;
        if (SizeJitter > 0)
        {
            float unusedJitter = (float)random.NextDouble() * SizeJitter; // Match DrawSingleStamp consumption
            currentScaleFactor = 1.0f + ((float)random.NextDouble() - 0.5f) * 2.0f * SizeJitter;
            if (currentScaleFactor < 0.1f) currentScaleFactor = 0.1f;
        }

        float currentRotationDelta = 0f;
        if (AngleJitter > 0)
        {
            currentRotationDelta = ((float)random.NextDouble() - 0.5f) * 2.0f * AngleJitter;
        }
        float baseRotation = (Rotations != null && i < Rotations.Count) ? Rotations[i] : 0f;
        float finalRotation = baseRotation + currentRotationDelta;

        var p = new SKPath(scaledPath);
        
        SKMatrix stampTransform = SKMatrix.CreateScale(currentScaleFactor, currentScaleFactor, 0, 0);
        stampTransform = stampTransform.PostConcat(SKMatrix.CreateRotationDegrees(finalRotation, 0, 0));
        p.Transform(stampTransform);

        p.Transform(SKMatrix.CreateTranslation(point.X, point.Y));
        combinedPath.AddPath(p);
      }
      combinedPath.Transform(TransformMatrix);
      return combinedPath;
    }

    public SKPath GetGeometryPath()
    {
        return GetPath(); // For stamps, the "geometry path" is the combined visual path.
    }
  }
}
