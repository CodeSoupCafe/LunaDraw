using SkiaSharp;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;

namespace LunaDraw.Logic.Managers
{
    public interface IToolStateManager
    {
        IDrawingTool ActiveTool { get; set; }
        SKColor StrokeColor { get; set; }
        SKColor? FillColor { get; set; }
        float StrokeWidth { get; set; }
        byte Opacity { get; set; }
        byte Flow { get; set; }
        float Spacing { get; set; }
        BrushShape CurrentBrushShape { get; set; }
        bool IsGlowEnabled { get; set; }
        SKColor GlowColor { get; set; }
        float GlowRadius { get; set; }
        bool IsRainbowEnabled { get; set; }
        float ScatterRadius { get; set; }
        float SizeJitter { get; set; }
        float AngleJitter { get; set; }
        float HueJitter { get; set; }
        List<IDrawingTool> AvailableTools { get; }
        List<BrushShape> AvailableBrushShapes { get; }
    }
}
