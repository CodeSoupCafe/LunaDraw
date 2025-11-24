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

        private SKPoint _startPoint;
        private DrawableLine? _currentLine;

        public void OnTouchPressed(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true) return;

            _startPoint = point;
            _currentLine = new DrawableLine
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
            if (context.CurrentLayer?.IsLocked == true || _currentLine == null) return;

            _currentLine.EndPoint = point - _startPoint;
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchReleased(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentLine == null) return;

            if (!_currentLine.EndPoint.Equals(SKPoint.Empty))
            {
                context.CurrentLayer.Elements.Add(_currentLine);
                MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
            }
            
            _currentLine = null;
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
        {
            if(_currentLine != null)
            {
                _currentLine.Draw(canvas);
            }
        }
    }
}
