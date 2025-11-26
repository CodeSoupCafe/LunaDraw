using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
    public class EraserBrushTool : IDrawingTool
    {
        public string Name => "Eraser";
        public ToolType Type => ToolType.Eraser;

        private SKPath? _currentPath;
        private DrawablePath? _currentDrawablePath;

        public void OnTouchPressed(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true) return;

            _currentPath = new SKPath();
            _currentPath.MoveTo(point);

            _currentDrawablePath = new DrawablePath
            {
                Path = _currentPath,
                StrokeColor = SKColors.Transparent, // Color doesn't matter for Clear, but transparent is logical
                StrokeWidth = context.StrokeWidth * 2, // Eraser usually wider
                Opacity = 255,
                BlendMode = SKBlendMode.Clear,
                ZIndex = context.CurrentLayer?.Elements.Count > 0 ? context.CurrentLayer.Elements.Max(e => e.ZIndex) + 1 : 0
            };

            context.CurrentLayer?.Elements.Add(_currentDrawablePath);
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchMoved(SKPoint point, ToolContext context)
        {
            if (_currentPath == null || context.CurrentLayer?.IsLocked == true) return;

            _currentPath.LineTo(point);
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchReleased(SKPoint point, ToolContext context)
        {
            if (_currentPath == null) return;

            _currentPath.LineTo(point);
            
            // Finalize
            MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());

            _currentPath = null;
            _currentDrawablePath = null;
        }

        public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
        {
            // Optional: Draw a circle cursor for eraser size
        }
    }
}
