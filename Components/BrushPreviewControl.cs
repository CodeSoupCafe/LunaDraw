using LunaDraw.Logic.Models;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LunaDraw.Components
{
    public class BrushPreviewControl : SKCanvasView
    {
        public static readonly BindableProperty BrushShapeProperty =
            BindableProperty.Create(nameof(BrushShape), typeof(BrushShape), typeof(BrushPreviewControl), null, propertyChanged: OnBrushShapeChanged);

        public BrushShape BrushShape
        {
            get => (BrushShape)GetValue(BrushShapeProperty);
            set => SetValue(BrushShapeProperty, value);
        }

        public static readonly BindableProperty ColorProperty =
             BindableProperty.Create(nameof(Color), typeof(Color), typeof(BrushPreviewControl), Colors.Black, propertyChanged: OnColorChanged);

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        private static void OnBrushShapeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((BrushPreviewControl)bindable).InvalidateSurface();
        }

        private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((BrushPreviewControl)bindable).InvalidateSurface();
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear();

            if (BrushShape?.Path == null) return;

            var info = e.Info;
            var center = new SKPoint(info.Width / 2, info.Height / 2);
            
            // Calculate scale to fit in the view (with padding)
            var bounds = BrushShape.Path.TightBounds;
            float maxDim = Math.Max(bounds.Width, bounds.Height);
            if (maxDim <= 0) maxDim = 1;
            
            float availableSize = Math.Min(info.Width, info.Height) * 0.6f; // 60% padding
            float scale = availableSize / maxDim;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor((byte)(Color.Red * 255), (byte)(Color.Green * 255), (byte)(Color.Blue * 255), (byte)(Color.Alpha * 255)),
                IsAntialias = true
            };

            canvas.Save();
            canvas.Translate(center.X, center.Y);
            canvas.Scale(scale);
            // Center the shape itself (if not centered at 0,0)
            canvas.Translate(-(bounds.MidX), -(bounds.MidY));
            
            canvas.DrawPath(BrushShape.Path, paint);
            canvas.Restore();
        }
    }
}
