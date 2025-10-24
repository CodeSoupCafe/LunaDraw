using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;
using SkiaSharp;
using System.Linq;

namespace LunaDraw.Logic.Tools
{
    public class SelectTool : IDrawingTool
    {
        public string Name => "Select";
        public ToolType Type => ToolType.Select;

        private SKPoint _lastPoint;
        private IDrawableElement? _draggedElement;

        public void OnTouchPressed(SKPoint point, ToolContext context)
        {
            _lastPoint = point;
            _draggedElement = null;

            var hitElement = context.AllElements
                                    .Where(e => e.IsVisible && !context.CurrentLayer.IsLocked)
                                    .OrderByDescending(e => e.ZIndex)
                                    .FirstOrDefault(e => e.HitTest(point));

            if (hitElement == null)
            {
                foreach (var element in context.SelectedElements)
                {
                    element.IsSelected = false;
                }
                context.SelectedElements.Clear();
                MessageBus.Current.SendMessage(new SelectionChangedMessage(context.SelectedElements));
            }
            else
            {
                if (!hitElement.IsSelected)
                {
                    foreach (var element in context.SelectedElements)
                    {
                        element.IsSelected = false;
                    }
                    context.SelectedElements.Clear();
                    hitElement.IsSelected = true;
                    context.SelectedElements.Add(hitElement);
                    MessageBus.Current.SendMessage(new SelectionChangedMessage(context.SelectedElements));
                }
                _draggedElement = hitElement;
            }
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchMoved(SKPoint point, ToolContext context)
        {
            if (_draggedElement == null || context.CurrentLayer?.IsLocked == true) return;

            var delta = point - _lastPoint;
            foreach (var element in context.SelectedElements)
            {
                element.Translate(delta);
            }
            _lastPoint = point;
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchReleased(SKPoint point, ToolContext context)
        {
            _draggedElement = null;
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
        {
        }
    }
}
