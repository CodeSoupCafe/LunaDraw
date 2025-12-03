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

      // Calculate rotation from CanvasMatrix
      float rotationRadians = (float)Math.Atan2(context.CanvasMatrix.SkewY, context.CanvasMatrix.ScaleX);
      float rotationDegrees = rotationRadians * 180f / (float)Math.PI;

      // Create alignment matrices
      var toAligned = SKMatrix.CreateRotationDegrees(rotationDegrees);
      var toWorld = SKMatrix.CreateRotationDegrees(-rotationDegrees);

      var p1 = toAligned.MapPoint(_startPoint);
      var p2 = toAligned.MapPoint(point);

      var left = Math.Min(p1.X, p2.X);
      var top = Math.Min(p1.Y, p2.Y);
      var right = Math.Max(p1.X, p2.X);
      var bottom = Math.Max(p1.Y, p2.Y);

      var width = right - left;
      var height = bottom - top;

      // The Top-Left corner in aligned space
      var alignedTL = new SKPoint(left, top);

      // Transform aligned Top-Left back to World space
      var worldTL = toWorld.MapPoint(alignedTL);

      var translation = SKMatrix.CreateTranslation(worldTL.X, worldTL.Y);

      _currentEllipse.TransformMatrix = SKMatrix.Concat(translation, toWorld);
      _currentEllipse.Oval = new SKRect(0, 0, width, height);

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
