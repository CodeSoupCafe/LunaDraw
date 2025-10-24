using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Represents a group of drawable elements that can be manipulated as a single unit.
    /// </summary>
    public class DrawableGroup : IDrawableElement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public List<IDrawableElement> Children { get; } = new List<IDrawableElement>();

        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public byte Opacity { get; set; } = 255;
        public SKColor? FillColor { get; set; } // Not directly used
        public SKColor StrokeColor { get; set; } // Not directly used
        public float StrokeWidth { get; set; } // Not directly used

        public SKRect Bounds
        {
            get
            {
                if (!Children.Any()) return SKRect.Empty;

                var left = Children.Min(c => c.Bounds.Left);
                var top = Children.Min(c => c.Bounds.Top);
                var right = Children.Max(c => c.Bounds.Right);
                var bottom = Children.Max(c => c.Bounds.Bottom);

                return new SKRect(left, top, right, bottom);
            }
        }

        public void Draw(SKCanvas canvas)
        {
            if (!IsVisible) return;

            foreach (var child in Children)
            {
                child.Draw(canvas);
            }

            if (IsSelected)
            {
                DrawSelectionIndicator(canvas);
            }
        }

        public bool HitTest(SKPoint point)
        {
            return Children.Any(child => child.HitTest(point));
        }

        public IDrawableElement Clone()
        {
            var newGroup = new DrawableGroup
            {
                IsVisible = IsVisible,
                IsSelected = false,
                ZIndex = ZIndex,
                Opacity = Opacity
            };
            foreach (var child in Children)
            {
                newGroup.Children.Add(child.Clone());
            }
            return newGroup;
        }

        public void Translate(SKPoint offset)
        {
            foreach (var child in Children)
            {
                child.Translate(offset);
            }
        }

        public void Transform(SKMatrix matrix)
        {
            foreach (var child in Children)
            {
                child.Transform(matrix);
            }
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
