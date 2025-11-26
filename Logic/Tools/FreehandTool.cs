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

    private SKPath? _currentPath;
    private SKPoint _pathOrigin;

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      _pathOrigin = point;
      _currentPath = new SKPath();
      _currentPath.MoveTo(0, 0); // Path starts at local origin
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true || _currentPath == null) return;

      var localPoint = point - _pathOrigin;
      _currentPath.LineTo(localPoint);
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true || _currentPath == null) return;

      var localPoint = point - _pathOrigin;
      _currentPath.LineTo(localPoint);

      if (!context.CurrentLayer.IsLocked && !_currentPath.IsEmpty)
      {
        var drawablePath = new DrawablePath
        {
          Path = _currentPath,
          TransformMatrix = SKMatrix.CreateTranslation(_pathOrigin.X, _pathOrigin.Y),
          StrokeColor = context.StrokeColor,
          StrokeWidth = context.StrokeWidth,
          Opacity = context.Opacity,
          FillColor = context.FillColor
        };
        context.CurrentLayer.Elements.Add(drawablePath);
        MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
      }

      _currentPath = null;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
      if (_currentPath == null) return;

      canvas.Save();
      canvas.Translate(_pathOrigin.X, _pathOrigin.Y);

      using var paint = new SKPaint
      {
        Style = SKPaintStyle.Stroke,
        Color = viewModel.StrokeColor.WithAlpha(viewModel.Opacity),
        StrokeWidth = viewModel.StrokeWidth,
        IsAntialias = true
      };

      canvas.DrawPath(_currentPath, paint);
      canvas.Restore();
    }
  }
}
