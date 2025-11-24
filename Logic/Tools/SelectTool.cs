using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace LunaDraw.Logic.Tools
{
    public enum SelectionState { None, Selecting, Dragging, Resizing }
    public enum ResizeHandle { None, TopLeft, TopRight, BottomLeft, BottomRight, Top, Right, Bottom, Left }

    public class SelectTool : IDrawingTool
    {
        public string Name => "Select";
        public ToolType Type => ToolType.Select;

        private SKPoint _lastPoint;
        private SelectionState _currentState = SelectionState.None;
        private ResizeHandle _activeHandle = ResizeHandle.None;
        private SKRect _originalBounds;
        private Dictionary<IDrawableElement, SKMatrix> _originalTransforms;
        private SKPoint _resizeStartPoint;

        public void OnTouchPressed(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true) return;

            var hitElement = context.AllElements
                                    .Where(e => e.IsVisible)
                                    .OrderByDescending(e => e.ZIndex)
                                    .FirstOrDefault(e => e.HitTest(point));

            if (hitElement != null)
            {
                if (!context.SelectionManager.Contains(hitElement))
                {
                    context.SelectionManager.Clear();
                    context.SelectionManager.Add(hitElement);
                }
                // If the element is already selected, we do nothing, allowing it to be dragged.
            }
            else
            {
                context.SelectionManager.Clear();
            }

            _lastPoint = point;
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void OnTouchMoved(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true) return;
            
            switch (_currentState)
            {
                case SelectionState.Dragging:
                    var delta = point - _lastPoint;
                    foreach (var element in context.SelectionManager.GetAll())
                    {
                        element.Translate(delta);
                    }
                    _lastPoint = point;
                    MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
                    break;

                case SelectionState.Resizing:
                    PerformResize(point, context);
                    MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
                    break;
            }
        }

        public void OnTouchReleased(SKPoint point, ToolContext context)
        {
            if (_currentState == SelectionState.Dragging || _currentState == SelectionState.Resizing)
            {
                MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
            }

            _currentState = SelectionState.None;
            _activeHandle = ResizeHandle.None;
            _originalTransforms?.Clear();
            MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
        {
            if (viewModel.SelectionManager.Selected.Any())
            {
                var bounds = viewModel.SelectionManager.GetBounds();
                if (bounds.IsEmpty) return;

                // Draw selection rectangle
                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.DodgerBlue,
                    StrokeWidth = 1,
                    PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0)
                };
                canvas.DrawRect(bounds, paint);

                // Draw resize handles
                if (_currentState != SelectionState.Resizing)
                {
                    DrawResizeHandle(canvas, new SKPoint(bounds.Left, bounds.Top));
                    DrawResizeHandle(canvas, new SKPoint(bounds.Right, bounds.Top));
                    DrawResizeHandle(canvas, new SKPoint(bounds.Left, bounds.Bottom));
                    DrawResizeHandle(canvas, new SKPoint(bounds.Right, bounds.Bottom));
                    DrawResizeHandle(canvas, new SKPoint(bounds.MidX, bounds.Top));
                    DrawResizeHandle(canvas, new SKPoint(bounds.Right, bounds.MidY));
                    DrawResizeHandle(canvas, new SKPoint(bounds.MidX, bounds.Bottom));
                    DrawResizeHandle(canvas, new SKPoint(bounds.Left, bounds.MidY));
                }
            }
        }

        private ResizeHandle GetResizeHandle(SKPoint point, SKRect bounds)
        {
            const float handleSize = 8f;
            if (IsPointNear(point, new SKPoint(bounds.Left, bounds.Top), handleSize)) return ResizeHandle.TopLeft;
            if (IsPointNear(point, new SKPoint(bounds.Right, bounds.Top), handleSize)) return ResizeHandle.TopRight;
            if (IsPointNear(point, new SKPoint(bounds.Left, bounds.Bottom), handleSize)) return ResizeHandle.BottomLeft;
            if (IsPointNear(point, new SKPoint(bounds.Right, bounds.Bottom), handleSize)) return ResizeHandle.BottomRight;
            if (IsPointNear(point, new SKPoint(bounds.MidX, bounds.Top), handleSize)) return ResizeHandle.Top;
            if (IsPointNear(point, new SKPoint(bounds.Right, bounds.MidY), handleSize)) return ResizeHandle.Right;
            if (IsPointNear(point, new SKPoint(bounds.MidX, bounds.Bottom), handleSize)) return ResizeHandle.Bottom;
            if (IsPointNear(point, new SKPoint(bounds.Left, bounds.MidY), handleSize)) return ResizeHandle.Left;
            return ResizeHandle.None;
        }

        private bool IsPointNear(SKPoint p1, SKPoint p2, float tolerance)
        {
            return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) < tolerance * tolerance;
        }

        private void PerformResize(SKPoint currentPoint, ToolContext context)
        {
            if (_originalTransforms == null) return;

            var dragDelta = currentPoint - _resizeStartPoint;
            var newBounds = CalculateNewBounds(_originalBounds, _activeHandle, dragDelta);

            if (newBounds.Width < 5 || newBounds.Height < 5) return;

            var sx = _originalBounds.Width == 0 ? 1 : newBounds.Width / _originalBounds.Width;
            var sy = _originalBounds.Height == 0 ? 1 : newBounds.Height / _originalBounds.Height;
            var tx = newBounds.Left - (_originalBounds.Left * sx);
            var ty = newBounds.Top - (_originalBounds.Top * sy);
            
            var transformFromOriginal = new SKMatrix(sx, 0, tx, 0, sy, ty, 0, 0, 1);

            foreach (var element in context.SelectionManager.GetAll())
            {
                if (_originalTransforms.TryGetValue(element, out var originalMatrix))
                {
                    element.TransformMatrix = SKMatrix.Concat(originalMatrix, transformFromOriginal);
                }
            }
        }
        
        private SKRect CalculateNewBounds(SKRect bounds, ResizeHandle handle, SKPoint dragDelta)
        {
            float left = bounds.Left, top = bounds.Top, right = bounds.Right, bottom = bounds.Bottom;

            switch (handle)
            {
                case ResizeHandle.TopLeft:    left += dragDelta.X; top += dragDelta.Y; break;
                case ResizeHandle.Top:        top += dragDelta.Y; break;
                case ResizeHandle.TopRight:   right += dragDelta.X; top += dragDelta.Y; break;
                case ResizeHandle.Left:       left += dragDelta.X; break;
                case ResizeHandle.Right:      right += dragDelta.X; break;
                case ResizeHandle.BottomLeft: left += dragDelta.X; bottom += dragDelta.Y; break;
                case ResizeHandle.Bottom:     bottom += dragDelta.Y; break;
                case ResizeHandle.BottomRight:right += dragDelta.X; bottom += dragDelta.Y; break;
            }
            
            if (left > right) { var temp = left; left = right; right = temp; }
            if (top > bottom) { var temp = top; top = bottom; bottom = temp; }

            return new SKRect(left, top, right, bottom);
        }

        private void DrawResizeHandle(SKCanvas canvas, SKPoint point)
        {
            const float handleSize = 4f;
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White
            };
            canvas.DrawCircle(point, handleSize, paint);
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = SKColors.DodgerBlue;
            paint.StrokeWidth = 1;
            canvas.DrawCircle(point, handleSize, paint);
        }
    }
}
