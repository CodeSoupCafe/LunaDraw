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
        public SKMatrix TransformMatrix { get; set; } = SKMatrix.CreateIdentity();

        public bool IsVisible { get; set; } = true;
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                foreach (var child in Children)
                {
                    child.IsSelected = value;
                }
            }
        }
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

            // The group's transform is applied to children, not to the canvas here
            foreach (var child in Children)
            {
                child.Draw(canvas);
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
                TransformMatrix = TransformMatrix,
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
            var matrix = SKMatrix.CreateTranslation(offset.X, offset.Y);
            Transform(matrix);
        }

        public void Transform(SKMatrix matrix)
        {
            // Apply the transformation to all children
            foreach (var child in Children)
            {
                child.Transform(matrix);
            }
        }

    }
}
