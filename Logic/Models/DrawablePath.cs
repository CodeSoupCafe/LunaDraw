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
        public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public byte Opacity { get; set; } = 255;
        public SKColor? FillColor { get; set; }
        public SKColor StrokeColor { get; set; }
        public float StrokeWidth { get; set; }

        public SKRect Bounds => TransformMatrix.MapRect(Path?.TightBounds ?? SKRect.Empty);

        public void Draw(SKCanvas canvas)
        {
            if (!IsVisible || Path == null) return;

            canvas.Save();
            var matrix = TransformMatrix;
            canvas.Concat(ref matrix);

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
                canvas.DrawPath(Path, highlightPaint);
            }

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = StrokeColor.WithAlpha(Opacity),
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            };

            canvas.DrawPath(Path, paint);
            canvas.Restore();
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
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth + 5 // Add tolerance
            };
            using var strokedPath = new SKPath();
            paint.GetFillPath(Path, strokedPath);
            return strokedPath.Contains(localPoint.X, localPoint.Y);
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
    }
}
