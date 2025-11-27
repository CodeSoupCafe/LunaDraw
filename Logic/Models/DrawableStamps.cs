using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Represents a series of stamped shapes (custom brush strokes).
    /// </summary>
    public class DrawableStamps : IDrawableElement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public List<SKPoint> Points { get; set; } = new List<SKPoint>();
        public BrushShape Shape { get; set; } = BrushShape.Circle();
        public float Size { get; set; } = 10f;
        
        // Flow controls the opacity of individual stamps.
        // 255 = full opacity per stamp (hard brush)
        // 25 = low opacity per stamp (soft/airbrush-like accumulation)
        public byte Flow { get; set; } = 255;

        public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public byte Opacity { get; set; } = 255; // Global opacity of the whole element
        public SKColor? FillColor { get; set; } // Not typically used for stamps, but maybe?
        public SKColor StrokeColor { get; set; } = SKColors.Black;
        public float StrokeWidth { get; set; } // Not used directly, using Size instead
        public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;
        public bool IsFilled { get; set; } = true; // Stamps are usually filled shapes

        public SKRect Bounds
        {
            get
            {
                if (Points == null || !Points.Any()) return SKRect.Empty;
                // Approximate bounds
                float halfSize = Size; // Since base shapes are approx radius 10, and we scale them
                // Actually, let's just calculate bounds of points and expand by Size
                float minX = Points.Min(p => p.X);
                float minY = Points.Min(p => p.Y);
                float maxX = Points.Max(p => p.X);
                float maxY = Points.Max(p => p.Y);
                var rect = new SKRect(minX - halfSize, minY - halfSize, maxX + halfSize, maxY + halfSize);
                return TransformMatrix.MapRect(rect);
            }
        }

        public void Draw(SKCanvas canvas)
        {
            if (!IsVisible || Points == null || !Points.Any() || Shape?.Path == null) return;

            canvas.Save();
            var matrix = TransformMatrix;
            canvas.Concat(ref matrix);

            // Draw selection highlight
            if (IsSelected)
            {
                // Simplified highlight: bounding box or just dots?
                // Let's draw a bounding box for simplicity
                using var highlightPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.DodgerBlue.WithAlpha(128),
                    StrokeWidth = 2,
                    IsAntialias = true
                };
                // Calculating bounds again is expensive, maybe cache?
                // For now, just draw the path of points?
                // Drawing individual highlights is too much.
                // Let's skip detailed highlight for now or do a simple path connect.
                var bounds = Bounds; // This uses Transformed bounds, so we need to inverse?
                // Actually Bounds property maps the rect.
                // Inside here we are already transformed.
                // So we need local bounds.
                float halfSize = Size;
                float minX = Points.Min(p => p.X);
                float minY = Points.Min(p => p.Y);
                float maxX = Points.Max(p => p.X);
                float maxY = Points.Max(p => p.Y);
                var localBounds = new SKRect(minX - halfSize, minY - halfSize, maxX + halfSize, maxY + halfSize);
                canvas.DrawRect(localBounds, highlightPaint);
            }

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill, // Stamps are usually filled
                Color = StrokeColor.WithAlpha((byte)(Flow * (Opacity / 255f))), // Combine Flow and Opacity
                IsAntialias = true,
                BlendMode = BlendMode
            };

            // Scale factor: standard shape is ~20 units wide (-10 to 10).
            // If Size is 20, scale is 1.
            // If Size is 10, scale is 0.5.
            float scale = Size / 20f;

            // Optimize: Create a scaled path once
            using var scaledPath = new SKPath(Shape.Path);
            var scaleMatrix = SKMatrix.CreateScale(scale, scale);
            scaledPath.Transform(scaleMatrix);

            foreach (var point in Points)
            {
                // Translate path to point
                // efficient way:
                canvas.Save();
                canvas.Translate(point.X, point.Y);
                canvas.DrawPath(scaledPath, paint);
                canvas.Restore();
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
