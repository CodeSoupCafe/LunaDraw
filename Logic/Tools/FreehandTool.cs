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

        public void OnTouchPressed(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true) return;

            _currentPath = new SKPath();
            _currentPath.MoveTo(point);
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchMoved(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentPath == null) return;

            _currentPath.LineTo(point);
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchReleased(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentPath == null) return;

            _currentPath.LineTo(point);

            var drawablePath = new DrawablePath
            {
                Path = _currentPath,
                StrokeColor = context.StrokeColor,
                StrokeWidth = context.StrokeWidth,
                Opacity = context.Opacity,
                FillColor = context.FillColor
            };

            context.CurrentLayer?.Elements.Add(drawablePath);
            if (context.CurrentLayer != null)
            {
                MessageBus.Current.SendMessage(new ElementAddedMessage(drawablePath, context.CurrentLayer));
            }
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());

            _currentPath = null;
        }

        public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
        {
            if (_currentPath == null) return;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = viewModel.StrokeColor.WithAlpha(viewModel.Opacity),
                StrokeWidth = viewModel.StrokeWidth,
                IsAntialias = true
            };

            canvas.DrawPath(_currentPath, paint);
        }
    }
}
