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
        public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;
        public bool IsFilled { get; set; }

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
                Style = IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                Color = StrokeColor.WithAlpha(Opacity),
                StrokeWidth = StrokeWidth,
                IsAntialias = true,
                BlendMode = BlendMode
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
                IsFilled = IsFilled
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
    }
}
