using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class FillTool : IDrawingTool
  {
    public string Name => "Fill";
    public ToolType Type => ToolType.Fill;

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      var hitElement = context.AllElements
                              .Where(e => e.IsVisible)
                              .OrderByDescending(e => e.ZIndex)
                              .FirstOrDefault(e => e.HitTest(point));

      if (hitElement != null)
      {
        hitElement.FillColor = context.FillColor;
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
      }
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
    }
  }
}
