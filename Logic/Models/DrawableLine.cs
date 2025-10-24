using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Represents a line shape on the canvas.
    /// </summary>
    public class DrawableLine : IDrawableElement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public SKPoint StartPoint { get; set; }
        public SKPoint EndPoint { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public byte Opacity { get; set; } = 255;
        public SKColor? FillColor { get; set; } // Not used for line
        public SKColor StrokeColor { get; set; }
        public float StrokeWidth { get; set; }

        public SKRect Bounds
        {
            get
            {
                return new SKRect(
                    System.Math.Min(StartPoint.X, EndPoint.X),
                    System.Math.Min(StartPoint.Y, EndPoint.Y),
                    System.Math.Max(StartPoint.X, EndPoint.X),
                    System.Math.Max(StartPoint.Y, EndPoint.Y)
                );
            }
        }

        public void Draw(SKCanvas canvas)
        {
            if (!IsVisible) return;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = StrokeColor.WithAlpha(Opacity),
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            };
            canvas.DrawLine(StartPoint, EndPoint, paint);

            if (IsSelected)
            {
                DrawSelectionIndicator(canvas);
            }
        }

        public bool HitTest(SKPoint point)
        {
            // Use path-based hit testing for accuracy
            using var path = new SKPath();
            path.MoveTo(StartPoint);
            path.LineTo(EndPoint);

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth + 10 // Add tolerance
            };
            using var strokedPath = new SKPath();
            paint.GetFillPath(path, strokedPath);
            return strokedPath.Contains(point.X, point.Y);
        }

        public IDrawableElement Clone()
        {
            return new DrawableLine
            {
                StartPoint = StartPoint,
                EndPoint = EndPoint,
                IsVisible = IsVisible,
                IsSelected = false,
                ZIndex = ZIndex,
                Opacity = Opacity,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth
            };
        }

        public void Translate(SKPoint offset)
        {
            StartPoint = new SKPoint(StartPoint.X + offset.X, StartPoint.Y + offset.Y);
            EndPoint = new SKPoint(EndPoint.X + offset.X, EndPoint.Y + offset.Y);
        }

        public void Transform(SKMatrix matrix)
        {
            var points = new[] { StartPoint, EndPoint };
            matrix.MapPoints(points);
            StartPoint = points[0];
            EndPoint = points[1];
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

            DrawHandle(canvas, StartPoint);
            DrawHandle(canvas, EndPoint);
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
