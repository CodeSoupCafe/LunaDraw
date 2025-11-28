using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class EllipseTool : IDrawingTool
  {
    public string Name => "Ellipse";
    public ToolType Type => ToolType.Ellipse;

    private SKPoint _startPoint;
    private DrawableEllipse? _currentEllipse;

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      _startPoint = point;
      _currentEllipse = new DrawableEllipse
      {
        StrokeColor = context.StrokeColor,
        StrokeWidth = context.StrokeWidth,
        Opacity = context.Opacity,
        FillColor = context.FillColor
      };
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true || _currentEllipse == null) return;

      var left = Math.Min(_startPoint.X, point.X);
      var top = Math.Min(_startPoint.Y, point.Y);
      var right = Math.Max(_startPoint.X, point.X);
      var bottom = Math.Max(_startPoint.Y, point.Y);

      _currentEllipse.TransformMatrix = SKMatrix.CreateTranslation(left, top);
      _currentEllipse.Oval = new SKRect(0, 0, right - left, bottom - top);

      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer == null || context.CurrentLayer.IsLocked || _currentEllipse == null) return;

      if (_currentEllipse.Oval.Width > 0 || _currentEllipse.Oval.Height > 0)
      {
        context.CurrentLayer.Elements.Add(_currentEllipse);
        MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
      }

      _currentEllipse = null;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchCancelled(ToolContext context)
    {
      _currentEllipse = null;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
      if (_currentEllipse != null)
      {
        _currentEllipse.Draw(canvas);
      }
    }
  }
}
