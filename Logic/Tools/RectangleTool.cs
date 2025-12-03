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

      // Calculate rotation from CanvasMatrix
      // SkewY is sin(angle) * scale, ScaleX is cos(angle) * scale
      float rotationRadians = (float)Math.Atan2(context.CanvasMatrix.SkewY, context.CanvasMatrix.ScaleX);
      float rotationDegrees = rotationRadians * 180f / (float)Math.PI;

      // Create alignment matrices
      // We rotate points by the canvas rotation to align them with the screen axes
      // Note: Rotation is around (0,0)
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

      // Assemble transform: Translate to World TL, then Rotate by -Degrees (which matches toWorld)
      // We want Final = Translate(WorldTL) * Rotate(-Degrees)
      // So that Local(0,0) -> Rot(0) -> Translate -> WorldTL
      var translation = SKMatrix.CreateTranslation(worldTL.X, worldTL.Y);

      _currentRectangle.TransformMatrix = SKMatrix.Concat(translation, toWorld);
      _currentRectangle.Rectangle = new SKRect(0, 0, width, height);

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

    public void OnTouchCancelled(ToolContext context)
    {
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
