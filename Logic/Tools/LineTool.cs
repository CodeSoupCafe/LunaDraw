using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class LineTool : IDrawingTool
  {
    public string Name => "Line";
    public ToolType Type => ToolType.Line;

    private SKPoint startPoint;
    private DrawableLine? currentLine;
    private readonly IMessageBus messageBus;

    public LineTool(IMessageBus messageBus)
    {
        this.messageBus = messageBus;
    }

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      startPoint = point;
      currentLine = new DrawableLine
      {
        StartPoint = SKPoint.Empty,
        EndPoint = SKPoint.Empty,
        TransformMatrix = SKMatrix.CreateTranslation(point.X, point.Y),
        StrokeColor = context.StrokeColor,
        StrokeWidth = context.StrokeWidth,
        Opacity = context.Opacity
      };
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true || currentLine == null) return;

      currentLine.EndPoint = point - startPoint;
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer == null || context.CurrentLayer.IsLocked || currentLine == null) return;

      if (!currentLine.EndPoint.Equals(SKPoint.Empty))
      {
        context.CurrentLayer.Elements.Add(currentLine);
        messageBus.SendMessage(new DrawingStateChangedMessage());
      }

      currentLine = null;
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchCancelled(ToolContext context)
    {
      currentLine = null;
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, ToolContext context)
    {
      if (currentLine != null)
      {
        currentLine.Draw(canvas);
      }
    }
  }
}