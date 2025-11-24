using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Base interface for all drawable elements on the canvas.
    /// Supports selection, visibility, layering, and manipulation.
    /// </summary>
    public interface IDrawableElement
    {
        /// <summary>
        /// Unique identifier for this element.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Bounding rectangle of the element in world coordinates.
        /// </summary>
        SKRect Bounds { get; }

        /// <summary>
        /// The transformation matrix applied to the element.
        /// </summary>
        SKMatrix TransformMatrix { get; set; }

        /// <summary>
        /// Whether the element is visible on the canvas.
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Whether the element is currently selected.
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Z-index for layering (higher values drawn on top).
        /// </summary>
        int ZIndex { get; set; }

        /// <summary>
        /// Opacity of the element (0-255).
        /// </summary>
        byte Opacity { get; set; }

        /// <summary>
        /// Fill color for the element (null for no fill).
        /// </summary>
        SKColor? FillColor { get; set; }

        /// <summary>
        /// Stroke/border color for the element.
        /// </summary>
        SKColor StrokeColor { get; set; }

        /// <summary>
        /// Width of the stroke/border.
        /// </summary>
        float StrokeWidth { get; set; }

        /// <summary>
        /// Draws the element on the provided canvas.
        /// </summary>
        /// <param name="canvas">The SKCanvas to draw on.</param>
        void Draw(SKCanvas canvas);

        /// <summary>
        /// Tests if a point hits this element.
        /// </summary>
        /// <param name="point">The point to test in world coordinates.</param>
        /// <returns>True if the point intersects with the element.</returns>
        bool HitTest(SKPoint point);

        /// <summary>
        /// Creates a deep copy of this element.
        /// </summary>
        /// <returns>A cloned instance of the element.</returns>
        IDrawableElement Clone();

        /// <summary>
        /// Translates the element by the specified offset.
        /// </summary>
        /// <param name="offset">The offset to move by.</param>
        void Translate(SKPoint offset);

        /// <summary>
        /// Transforms the element using the provided matrix.
        /// </summary>
        /// <param name="matrix">The transformation matrix.</param>
        void Transform(SKMatrix matrix);
    }
}
