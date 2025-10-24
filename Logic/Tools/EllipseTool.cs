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
                Oval = new SKRect(point.X, point.Y, point.X, point.Y),
                StrokeColor = context.StrokeColor,
                StrokeWidth = context.StrokeWidth,
                Opacity = context.Opacity,
                FillColor = context.FillColor
            };
            context.CurrentLayer?.Elements.Add(_currentEllipse);
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchMoved(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentEllipse == null) return;

            _currentEllipse.Oval = new SKRect(
                System.Math.Min(_startPoint.X, point.X),
                System.Math.Min(_startPoint.Y, point.Y),
                System.Math.Max(_startPoint.X, point.X),
                System.Math.Max(_startPoint.Y, point.Y)
            );
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchReleased(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentEllipse == null) return;

            if (_currentEllipse.Oval.Width == 0 || _currentEllipse.Oval.Height == 0)
            {
                context.CurrentLayer?.Elements.Remove(_currentEllipse);
            }
            else if (context.CurrentLayer != null)
            {
                MessageBus.Current.SendMessage(new ElementAddedMessage(_currentEllipse, context.CurrentLayer));
            }
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
            _currentEllipse = null;
        }

        public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
        {
        }
    }
}
