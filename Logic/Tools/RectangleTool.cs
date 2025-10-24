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
                Rectangle = new SKRect(point.X, point.Y, point.X, point.Y),
                StrokeColor = context.StrokeColor,
                StrokeWidth = context.StrokeWidth,
                Opacity = context.Opacity,
                FillColor = context.FillColor
            };
            context.CurrentLayer?.Elements.Add(_currentRectangle);
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchMoved(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentRectangle == null) return;

            _currentRectangle.Rectangle = new SKRect(
                System.Math.Min(_startPoint.X, point.X),
                System.Math.Min(_startPoint.Y, point.Y),
                System.Math.Max(_startPoint.X, point.X),
                System.Math.Max(_startPoint.Y, point.Y)
            );
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchReleased(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentRectangle == null) return;

            if (_currentRectangle.Rectangle.Width == 0 || _currentRectangle.Rectangle.Height == 0)
            {
                context.CurrentLayer?.Elements.Remove(_currentRectangle);
            }
            else if (context.CurrentLayer != null)
            {
                MessageBus.Current.SendMessage(new ElementAddedMessage(_currentRectangle, context.CurrentLayer));
            }
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
            _currentRectangle = null;
        }

        public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
        {
        }
    }
}
