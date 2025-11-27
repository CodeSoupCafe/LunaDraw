using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class RectangleTool : IDrawingTool
  {
    public string Name => "Rectangle";
    public ToolType Type => ToolType.Rectangle;

    private SKPoint _startPoint;
    private DrawableRectangle? _currentRectangle;

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      _startPoint = point;
      _currentRectangle = new DrawableRectangle
      {
        StrokeColor = context.StrokeColor,
        StrokeWidth = context.StrokeWidth,
        Opacity = context.Opacity,
        FillColor = context.FillColor
      };
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true || _currentRectangle == null) return;

      var left = Math.Min(_startPoint.X, point.X);
      var top = Math.Min(_startPoint.Y, point.Y);
      var right = Math.Max(_startPoint.X, point.X);
      var bottom = Math.Max(_startPoint.Y, point.Y);

      _currentRectangle.TransformMatrix = SKMatrix.CreateTranslation(left, top);
      _currentRectangle.Rectangle = new SKRect(0, 0, right - left, bottom - top);

      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer == null || context.CurrentLayer.IsLocked || _currentRectangle == null) return;

      if (_currentRectangle.Rectangle.Width > 0 || _currentRectangle.Rectangle.Height > 0)
      {
        context.CurrentLayer.Elements.Add(_currentRectangle);
        MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
      }

      _currentRectangle = null;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
      if (_currentRectangle != null)
      {
        _currentRectangle.Draw(canvas);
      }
    }
  }
}
