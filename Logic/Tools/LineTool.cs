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
                StartPoint = point,
                EndPoint = point,
                StrokeColor = context.StrokeColor,
                StrokeWidth = context.StrokeWidth,
                Opacity = context.Opacity
            };
            context.CurrentLayer?.Elements.Add(_currentLine);
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchMoved(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentLine == null) return;

            _currentLine.EndPoint = point;
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchReleased(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || _currentLine == null) return;

            if (_currentLine.StartPoint == _currentLine.EndPoint)
            {
                context.CurrentLayer?.Elements.Remove(_currentLine);
            }
            else if (context.CurrentLayer != null)
            {
                MessageBus.Current.SendMessage(new ElementAddedMessage(_currentLine, context.CurrentLayer));
            }
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
            _currentLine = null;
        }

        public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
        {
        }
    }
}
