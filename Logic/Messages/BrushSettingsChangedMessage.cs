using SkiaSharp;

namespace LunaDraw.Logic.Messages
{
    /// <summary>
    /// Message sent when brush settings (color, transparency) change.
    /// </summary>
    public class BrushSettingsChangedMessage(
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
        float? hueJitter = null,
        bool shouldClearFillColor = false)
    {
        public SKColor? StrokeColor { get; } = strokeColor;
        public SKColor? FillColor { get; } = fillColor;
        public byte? Transparency { get; } = transparency;
        public byte? Flow { get; } = flow;
        public float? Spacing { get; } = spacing;
        public float? StrokeWidth { get; } = strokeWidth;
        public bool? IsGlowEnabled { get; } = isGlowEnabled;
        public SKColor? GlowColor { get; } = glowColor;
        public float? GlowRadius { get; } = glowRadius;
        public bool? IsRainbowEnabled { get; } = isRainbowEnabled;
        public float? ScatterRadius { get; } = scatterRadius;
        public float? SizeJitter { get; } = sizeJitter;
        public float? AngleJitter { get; } = angleJitter;
        public float? HueJitter { get; } = hueJitter;
        public bool ShouldClearFillColor { get; } = shouldClearFillColor;
    }
}
