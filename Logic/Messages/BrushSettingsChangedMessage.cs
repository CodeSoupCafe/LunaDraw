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
        public bool? IsRainbowEnabled { get; }
        public float? ScatterRadius { get; }
        public float? SizeJitter { get; }
        public float? AngleJitter { get; }
        public float? HueJitter { get; }

        public BrushSettingsChangedMessage(
            SKColor? strokeColor = null, 
            SKColor? fillColor = null, 
            byte? transparency = null, 
            byte? flow = null, 
            float? spacing = null, 
            float? strokeWidth = null, 
            bool? isGlowEnabled = null, 
            SKColor? glowColor = null, 
            float? glowRadius = null,
            bool? isRainbowEnabled = null,
            float? scatterRadius = null,
            float? sizeJitter = null,
            float? angleJitter = null,
            float? hueJitter = null)
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
            IsRainbowEnabled = isRainbowEnabled;
            ScatterRadius = scatterRadius;
            SizeJitter = sizeJitter;
            AngleJitter = angleJitter;
            HueJitter = hueJitter;
        }
    }
}
