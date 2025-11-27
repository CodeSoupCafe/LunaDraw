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
                StrokeColor = SKColors.White, // Visual preview color
                StrokeWidth = context.StrokeWidth * 2, // Eraser usually wider
                Opacity = 255,
                BlendMode = SKBlendMode.SrcOver,
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
            if (_currentPath == null || context.CurrentLayer == null) return;

            _currentPath.LineTo(point);

            // Remove the temporary "global" eraser path
            if (_currentDrawablePath != null)
            {
                context.CurrentLayer.Elements.Remove(_currentDrawablePath);
            }

            // Convert stroke to fill path (outline) for intersection
            using var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = context.StrokeWidth * 2,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };
            using var eraserOutline = new SKPath();
            strokePaint.GetFillPath(_currentPath, eraserOutline);

            var elements = context.CurrentLayer.Elements.ToList();
            var modified = false;

            foreach (var element in elements)
            {
                if (element == _currentDrawablePath) continue;
                if (!element.IsVisible) continue;

                // Get element geometry
                using var elementPath = element.GetPath();

                // Calculate intersection
                using var intersection = new SKPath();
                if (eraserOutline.Op(elementPath, SKPathOp.Intersect, intersection) && !intersection.IsEmpty)
                {
                    // Create the per-element eraser path
                    var perElementEraser = new DrawablePath
                    {
                        Path = new SKPath(intersection),
                        StrokeColor = SKColors.Transparent,
                        StrokeWidth = 0,
                        IsFilled = true,
                        BlendMode = SKBlendMode.Clear,
                        Opacity = 255,
                        ZIndex = int.MaxValue 
                    };

                    if (element is DrawableGroup group)
                    {
                        group.Children.Add(perElementEraser);
                    }
                    else
                    {
                        var newGroup = new DrawableGroup
                        {
                            ZIndex = element.ZIndex,
                            IsSelected = element.IsSelected
                        };
                        
                        context.CurrentLayer.Elements.Remove(element);
                        
                        newGroup.Children.Add(element);
                        newGroup.Children.Add(perElementEraser);
                        
                        context.CurrentLayer.Elements.Add(newGroup);
                    }
                    modified = true;
                }
            }
            
            if (modified)
            {
                MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
            }
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
