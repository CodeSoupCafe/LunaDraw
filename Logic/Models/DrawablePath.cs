using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Represents a freehand drawn path on the canvas.
    /// </summary>
    public class DrawablePath : IDrawableElement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public required SKPath Path { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public byte Opacity { get; set; } = 255;
        public SKColor? FillColor { get; set; }
        public SKColor StrokeColor { get; set; }
        public float StrokeWidth { get; set; }

        public SKRect Bounds => Path?.TightBounds ?? SKRect.Empty;

        public void Draw(SKCanvas canvas)
        {
            if (!IsVisible || Path == null) return;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = StrokeColor.WithAlpha(Opacity),
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            };

            canvas.DrawPath(Path, paint);

            // Draw selection indicator if selected
            if (IsSelected)
            {
                DrawSelectionIndicator(canvas);
            }
        }

        public bool HitTest(SKPoint point)
        {
            if (Path == null) return false;

            // Check if point is within bounds first (faster)
            if (!Bounds.Contains(point)) return false;

            // Check if path contains point with tolerance
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth + 5 // Add tolerance
            };
            using var strokedPath = new SKPath();
            paint.GetFillPath(Path, strokedPath);
            return strokedPath.Contains(point.X, point.Y);
        }

        public IDrawableElement Clone()
        {
            return new DrawablePath
            {
                Path = new SKPath(Path),
                IsVisible = IsVisible,
                IsSelected = false, // Don't clone selection state
                ZIndex = ZIndex,
                Opacity = Opacity,
                FillColor = FillColor,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth
            };
        }

        public void Translate(SKPoint offset)
        {
            if (Path == null) return;
            Path.Transform(SKMatrix.CreateTranslation(offset.X, offset.Y));
        }

        public void Transform(SKMatrix matrix)
        {
            if (Path == null) return;
            Path.Transform(matrix);
        }

        private void DrawSelectionIndicator(SKCanvas canvas)
        {
            var bounds = Bounds;
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 2,
                PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
            };
            canvas.DrawRect(bounds, paint);
        }
    }
}
