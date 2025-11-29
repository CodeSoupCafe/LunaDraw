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
        public bool? IsGlowEnabled { get; }
        public SKColor? GlowColor { get; }
        public float? GlowRadius { get; }

        public BrushSettingsChangedMessage(SKColor? strokeColor = null, SKColor? fillColor = null, byte? transparency = null, byte? flow = null, float? spacing = null, float? strokeWidth = null, bool? isGlowEnabled = null, SKColor? glowColor = null, float? glowRadius = null)
        {
            StrokeColor = strokeColor;
            FillColor = fillColor;
            Transparency = transparency;
            Flow = flow;
            Spacing = spacing;
            StrokeWidth = strokeWidth;
            IsGlowEnabled = isGlowEnabled;
            GlowColor = glowColor;
            GlowRadius = glowRadius;
        }
    }
}
