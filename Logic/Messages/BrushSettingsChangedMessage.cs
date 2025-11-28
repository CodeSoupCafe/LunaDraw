using SkiaSharp;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when brush settings (color, transparency) change.
    /// </summary>
    public class BrushSettingsChangedMessage
    {
        public SKColor? StrokeColor { get; }
        public SKColor? FillColor { get; }
        public byte? Transparency { get; }
        public byte? Flow { get; }
        public float? Spacing { get; }
        public float? StrokeWidth { get; }

        public BrushSettingsChangedMessage(SKColor? strokeColor = null, SKColor? fillColor = null, byte? transparency = null, byte? flow = null, float? spacing = null, float? strokeWidth = null)
        {
            StrokeColor = strokeColor;
            FillColor = fillColor;
            Transparency = transparency;
            Flow = flow;
            Spacing = spacing;
            StrokeWidth = strokeWidth;
        }
    }
}
