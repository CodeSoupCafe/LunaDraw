using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Represents a rectangle shape on the canvas.
    /// </summary>
    public class DrawableRectangle : IDrawableElement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public SKRect Rectangle { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public byte Opacity { get; set; } = 255;
        public SKColor? FillColor { get; set; }
        public SKColor StrokeColor { get; set; }
        public float StrokeWidth { get; set; }

        public SKRect Bounds => Rectangle;

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
                canvas.DrawRect(Rectangle, fillPaint);
            }

            // Draw stroke
            using var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = StrokeColor.WithAlpha(Opacity),
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            };
            canvas.DrawRect(Rectangle, strokePaint);

            // Draw selection indicator if selected
            if (IsSelected)
            {
                DrawSelectionIndicator(canvas);
            }
        }

        public bool HitTest(SKPoint point)
        {
            // Check if filled and point is inside
            if (FillColor.HasValue && Rectangle.Contains(point))
            {
                return true;
            }

            // Check if point is near stroke
            var tolerance = StrokeWidth + 5;
            var outerRect = new SKRect(
                Rectangle.Left - tolerance,
                Rectangle.Top - tolerance,
                Rectangle.Right + tolerance,
                Rectangle.Bottom + tolerance
            );
            var innerRect = new SKRect(
                Rectangle.Left + tolerance,
                Rectangle.Top + tolerance,
                Rectangle.Right - tolerance,
                Rectangle.Bottom - tolerance
            );

            return outerRect.Contains(point) && !innerRect.Contains(point);
        }

        public IDrawableElement Clone()
        {
            return new DrawableRectangle
            {
                Rectangle = Rectangle,
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
            Rectangle = new SKRect(
                Rectangle.Left + offset.X,
                Rectangle.Top + offset.Y,
                Rectangle.Right + offset.X,
                Rectangle.Bottom + offset.Y
            );
        }

        public void Transform(SKMatrix matrix)
        {
            var points = new[]
            {
                new SKPoint(Rectangle.Left, Rectangle.Top),
                new SKPoint(Rectangle.Right, Rectangle.Bottom)
            };
            matrix.MapPoints(points);
            Rectangle = new SKRect(points[0].X, points[0].Y, points[1].X, points[1].Y);
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
