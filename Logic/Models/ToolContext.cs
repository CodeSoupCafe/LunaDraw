using SkiaSharp;
using System.Collections.Generic;

namespace LunaDraw.Logic.Models
{
    /// <summary>
    /// Provides context information to drawing tools, including current drawing properties and access to elements.
    /// </summary>
    public class ToolContext
    {
        public required Layer CurrentLayer { get; set; }
        public SKColor StrokeColor { get; set; }
        public SKColor? FillColor { get; set; }
        public float StrokeWidth { get; set; }
        public byte Opacity { get; set; }
        public required IEnumerable<IDrawableElement> AllElements { get; set; }
        public IDrawableElement? SelectedElement { get; set; }
        public List<IDrawableElement> SelectedElements { get; set; } = new List<IDrawableElement>();
    }
}
