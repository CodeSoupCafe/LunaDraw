using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    public class DrawableImage(SKBitmap bitmap) : IDrawableElement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string? SourcePath { get; set; }
        public SKBitmap Bitmap { get; set; } = bitmap;
        public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public byte Opacity { get; set; } = 255;
        
        // Images don't typically use FillColor, but we satisfy the interface.
        // Could be used for tinting in the future.
        public SKColor? FillColor { get; set; } 
        
        // Stroke could be a border around the image
        public SKColor StrokeColor { get; set; } = SKColors.Transparent;
        public float StrokeWidth { get; set; } = 0;
        
        public bool IsGlowEnabled { get; set; } = false;
        public SKColor GlowColor { get; set; } = SKColors.Transparent;
        public float GlowRadius { get; set; } = 0f;

        public SKRect Bounds => TransformMatrix.MapRect(new SKRect(0, 0, Bitmap.Width, Bitmap.Height));

        public void Draw(SKCanvas canvas)
        {
            if (!IsVisible || Bitmap == null) return;

            canvas.Save();
            var matrix = TransformMatrix;
            canvas.Concat(in matrix);

            var bounds = new SKRect(0, 0, Bitmap.Width, Bitmap.Height);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White.WithAlpha(Opacity) // Alpha affects the bitmap draw
            };

            // Draw selection highlight
            if (IsSelected)
            {
                using var highlightPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.DodgerBlue.WithAlpha(128),
                    StrokeWidth = 4 / matrix.ScaleX, // Adjust for scale
                    IsAntialias = true
                };
                // Draw slightly outside
                var highlightRect = bounds;
                highlightRect.Inflate(2, 2);
                canvas.DrawRect(highlightRect, highlightPaint);
            }

            // Draw Glow
            if (IsGlowEnabled && GlowRadius > 0)
            {
                using var glowPaint = new SKPaint
                {
                    Style = SKPaintStyle.StrokeAndFill,
                    Color = GlowColor.WithAlpha(Opacity),
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, GlowRadius),
                    IsAntialias = true
                };
                // We draw the rect as the glow source
                canvas.DrawRect(bounds, glowPaint);
            }

            // Draw the Bitmap
            using (var image = SKImage.FromBitmap(Bitmap))
            {
                canvas.DrawImage(image, bounds, new SKSamplingOptions(SKFilterMode.Linear), paint);
            }

            // Draw Border if set
            if (StrokeWidth > 0 && StrokeColor.Alpha > 0)
            {
                using var borderPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = StrokeColor.WithAlpha(Opacity),
                    StrokeWidth = StrokeWidth,
                    IsAntialias = true
                };
                canvas.DrawRect(bounds, borderPaint);
            }

            canvas.Restore();
        }

        public bool HitTest(SKPoint point)
        {
            if (Bitmap == null) return false;

            if (!TransformMatrix.TryInvert(out var inverseMatrix))
                return false;

            var localPoint = inverseMatrix.MapPoint(point);
            var bounds = new SKRect(0, 0, Bitmap.Width, Bitmap.Height);

            return bounds.Contains(localPoint);
        }

        public IDrawableElement Clone()
        {
            // Shallow copy of bitmap is usually sufficient unless we edit pixels.
            // If we needed deep copy: Bitmap.Copy()
            return new DrawableImage(Bitmap)
            {
                TransformMatrix = TransformMatrix,
                IsVisible = IsVisible,
                IsSelected = false, // Clones usually start unselected
                ZIndex = ZIndex,
                Opacity = Opacity,
                FillColor = FillColor,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                IsGlowEnabled = IsGlowEnabled,
                GlowColor = GlowColor,
                GlowRadius = GlowRadius
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
            // Return bounding path
            var path = new SKPath();
            if (Bitmap != null)
            {
                path.AddRect(new SKRect(0, 0, Bitmap.Width, Bitmap.Height));
            }
            path.Transform(TransformMatrix);
            return path;
        }

        public SKPath GetGeometryPath()
        {
            var path = new SKPath();
            if (Bitmap != null)
            {
                path.AddRect(new SKRect(0, 0, Bitmap.Width, Bitmap.Height));
            }
            path.Transform(TransformMatrix);
            return path;
        }
    }
}
