using LunaDraw.Logic.Managers;
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
        public byte Flow { get; set; }
        public float Spacing { get; set; }
        public required BrushShape BrushShape { get; set; }
        public required IEnumerable<IDrawableElement> AllElements { get; set; }
        public IEnumerable<Layer> Layers { get; set; }
        public required SelectionManager SelectionManager { get; set; }
        public float Scale { get; set; } = 1.0f;
        public bool IsGlowEnabled { get; init; }
        public SKColor GlowColor { get; init; }
        public float GlowRadius { get; init; }
        public bool IsRainbowEnabled { get; init; }
        public float ScatterRadius { get; init; }
        public float SizeJitter { get; init; }
        public float AngleJitter { get; init; }
        public float HueJitter { get; init; }
        public SKMatrix CanvasMatrix { get; set; } = SKMatrix.CreateIdentity();
    }
}
