using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class FreehandTool : IDrawingTool
  {
    public string Name => "Freehand";
    public ToolType Type => ToolType.Freehand;

    private List<SKPoint>? _currentPoints;
    private SKPoint _lastStampPoint;
    private bool _isDrawing;

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      _currentPoints =
      [
        // Add initial point
        point,
      ];
      _lastStampPoint = point;
      _isDrawing = true;

      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (!_isDrawing || context.CurrentLayer?.IsLocked == true || _currentPoints == null) return;

      float spacingPixels = context.Spacing * context.StrokeWidth;
      if (spacingPixels < 1) spacingPixels = 1;

      var vector = point - _lastStampPoint;
      float distance = vector.Length;

      if (distance >= spacingPixels)
      {
        var direction = vector;
        // Normalize manually to avoid issues with zero length
        if (distance > 0)
        {
          float invLength = 1.0f / distance;
          direction = new SKPoint(direction.X * invLength, direction.Y * invLength);
        }

        int steps = (int)(distance / spacingPixels);
        for (int i = 0; i < steps; i++)
        {
          var newPoint = _lastStampPoint + new SKPoint(direction.X * spacingPixels, direction.Y * spacingPixels);
          _currentPoints.Add(newPoint);
          _lastStampPoint = newPoint;
        }

        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());

      }
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (!_isDrawing || context.CurrentLayer == null || context.CurrentLayer.IsLocked || _currentPoints == null) return;

      if (_currentPoints.Count > 0)
      {
        var element = new DrawableStamps
        {
          Points = new List<SKPoint>(_currentPoints),
          Shape = context.BrushShape,
          Size = context.StrokeWidth,
          Flow = context.Flow,
          Opacity = context.Opacity,
          StrokeColor = context.StrokeColor,
          IsGlowEnabled = context.IsGlowEnabled,
          GlowColor = context.GlowColor,
          GlowRadius = context.GlowRadius,
        };

        context.CurrentLayer.Elements.Add(element);
        MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
      }

      _currentPoints = null;
      _isDrawing = false;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchCancelled(ToolContext context)
    {
      _currentPoints = null;
      _isDrawing = false;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
      if (_currentPoints == null || _currentPoints.Count == 0) return;

      // Get current shape from viewModel
      var shape = viewModel.CurrentBrushShape;
      if (shape?.Path == null) return;

      float size = viewModel.StrokeWidth;
      float scale = size / 20f;
      byte flow = viewModel.Flow;
      byte opacity = viewModel.Opacity;

      using var paint = new SKPaint
      {
        Style = SKPaintStyle.Fill,
        Color = viewModel.StrokeColor.WithAlpha((byte)(flow * (opacity / 255f))),
        IsAntialias = true
      };

      using var scaledPath = new SKPath(shape.Path);
      var scaleMatrix = SKMatrix.CreateScale(scale, scale);
      scaledPath.Transform(scaleMatrix);

      foreach (var point in _currentPoints)
      {
        canvas.Save();
        canvas.Translate(point.X, point.Y);
        canvas.DrawPath(scaledPath, paint);
        canvas.Restore();
      }
    }
  }
}