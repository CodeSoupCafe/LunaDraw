using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class EraserTool : IDrawingTool
  {
    public string Name => "Eraser";
    public ToolType Type => ToolType.Eraser;

    private bool isErasing;
    private readonly IMessageBus messageBus;

    public EraserTool(IMessageBus messageBus)
    {
        this.messageBus = messageBus;
    }

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      isErasing = true;
      Erase(point, context);
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (isErasing)
      {
        Erase(point, context);
      }
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      isErasing = false;
    }

    public void OnTouchCancelled(ToolContext context)
    {
      isErasing = false;
    }

    private void Erase(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      var hitElement = context.AllElements
                              .Where(e => e.IsVisible)
                              .OrderByDescending(e => e.ZIndex)
                              .FirstOrDefault(e => e.HitTest(point));

      if (hitElement != null && context.CurrentLayer != null)
      {
        context.CurrentLayer.Elements.Remove(hitElement);
        messageBus.SendMessage(new DrawingStateChangedMessage());
        messageBus.SendMessage(new CanvasInvalidateMessage());
      }
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
    }
  }
}