using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public enum ToolType
  {
    None,
    Select,
    Freehand,
    Rectangle,
    Ellipse,
    Line,
    Fill,
    Eraser
  }

  public interface IDrawingTool
  {
    string Name { get; }
    ToolType Type { get; }

    void OnTouchPressed(SKPoint point, ToolContext context);
    void OnTouchMoved(SKPoint point, ToolContext context);
    void OnTouchReleased(SKPoint point, ToolContext context);
    void OnTouchCancelled(ToolContext context);
    void DrawPreview(SKCanvas canvas, MainViewModel viewModel);
  }
}
