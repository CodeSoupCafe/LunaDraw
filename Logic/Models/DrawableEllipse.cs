using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Represents an ellipse shape on the canvas.
    /// </summary>
    public class DrawableEllipse : IDrawableElement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public SKRect Oval { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public byte Opacity { get; set; } = 255;
        public SKColor? FillColor { get; set; }
        public SKColor StrokeColor { get; set; }
        public float StrokeWidth { get; set; }

        public SKRect Bounds => Oval;

        public void Draw(SKCanvas canvas)
        {
            if (!IsVisible) return;

            // Draw fill if specified
            if (FillColor.HasValue)
            {
                using var fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = FillColor.Value.WithAlpha(Opacity),
                    IsAntialias = true
                };
                canvas.DrawOval(Oval, fillPaint);
            }

            // Draw stroke
            using var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = StrokeColor.WithAlpha(Opacity),
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            };
            canvas.DrawOval(Oval, strokePaint);

            // Draw selection indicator if selected
            if (IsSelected)
            {
                DrawSelectionIndicator(canvas);
            }
        }

        public bool HitTest(SKPoint point)
        {
            // Use path-based hit testing for accuracy
            using var path = new SKPath();
            path.AddOval(Oval);

            // Check if filled and point is inside
            if (FillColor.HasValue && path.Contains(point.X, point.Y))
            {
                return true;
            }

            // Check if point is near stroke
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth + 5 // Add tolerance
            };
            using var strokedPath = new SKPath();
            paint.GetFillPath(path, strokedPath);
            return strokedPath.Contains(point.X, point.Y);
        }

        public IDrawableElement Clone()
        {
            return new DrawableEllipse
            {
                Oval = Oval,
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
            Oval = new SKRect(
                Oval.Left + offset.X,
                Oval.Top + offset.Y,
                Oval.Right + offset.X,
                Oval.Bottom + offset.Y
            );
        }

        public void Transform(SKMatrix matrix)
        {
            var points = new[]
            {
                new SKPoint(Oval.Left, Oval.Top),
                new SKPoint(Oval.Right, Oval.Bottom)
            };
            matrix.MapPoints(points);
            Oval = new SKRect(points[0].X, points[0].Y, points[1].X, points[1].Y);
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

            // Draw corner handles
            DrawHandle(canvas, new SKPoint(bounds.Left, bounds.Top));
            DrawHandle(canvas, new SKPoint(bounds.Right, bounds.Top));
            DrawHandle(canvas, new SKPoint(bounds.Left, bounds.Bottom));
            DrawHandle(canvas, new SKPoint(bounds.Right, bounds.Bottom));
        }

        private void DrawHandle(SKCanvas canvas, SKPoint point)
        {
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White
            };
            canvas.DrawCircle(point, 5, paint);

            using var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 2
            };
            canvas.DrawCircle(point, 5, strokePaint);
        }
    }
}
