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
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer == null || context.CurrentLayer.IsLocked || currentLine == null) return;

      if (!currentLine.EndPoint.Equals(SKPoint.Empty))
      {
        context.CurrentLayer.Elements.Add(currentLine);
        MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
      }

      currentLine = null;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchCancelled(ToolContext context)
    {
      currentLine = null;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
      if (currentLine != null)
      {
        currentLine.Draw(canvas);
      }
    }
  }
}