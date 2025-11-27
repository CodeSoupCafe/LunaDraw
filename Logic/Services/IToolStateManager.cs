using SkiaSharp;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;

namespace LunaDraw.Logic.Services
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
        List<IDrawingTool> AvailableTools { get; }
        List<BrushShape> AvailableBrushShapes { get; }
    }
}
