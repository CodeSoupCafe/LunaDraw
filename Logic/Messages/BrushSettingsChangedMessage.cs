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

        public BrushSettingsChangedMessage(SKColor? strokeColor = null, SKColor? fillColor = null, byte? transparency = null)
        {
            StrokeColor = strokeColor;
            FillColor = fillColor;
            Transparency = transparency;
        }
    }
}
