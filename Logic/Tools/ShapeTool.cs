using LunaDraw.Logic.Extensions;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;
using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
    public abstract class ShapeTool<T>(IMessageBus messageBus) : IDrawingTool where T : class, IDrawableElement
    {
        public abstract string Name { get; }
        public abstract ToolType Type { get; }

        protected readonly IMessageBus MessageBus = messageBus;
        protected SKPoint StartPoint;
        protected T? CurrentShape;

        protected abstract T CreateShape(ToolContext context);
        protected abstract void UpdateShape(T shape, SKRect bounds, SKMatrix transform);
        protected abstract bool IsShapeValid(T shape);

        public virtual void OnTouchPressed(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true) return;

            StartPoint = point;
            CurrentShape = CreateShape(context);
        }

        public virtual void OnTouchMoved(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer?.IsLocked == true || CurrentShape == null) return;

            var (transform, bounds) = context.CanvasMatrix.CalculateRotatedBounds(StartPoint, point);
            UpdateShape(CurrentShape, bounds, transform);

            MessageBus.SendMessage(new CanvasInvalidateMessage());
        }

        public virtual void OnTouchReleased(SKPoint point, ToolContext context)
        {
            if (context.CurrentLayer == null || context.CurrentLayer.IsLocked || CurrentShape == null) return;

            if (IsShapeValid(CurrentShape))
            {
                context.CurrentLayer.Elements.Add(CurrentShape);
                MessageBus.SendMessage(new DrawingStateChangedMessage());
            }

            CurrentShape = null;
            MessageBus.SendMessage(new CanvasInvalidateMessage());
        }

        public virtual void OnTouchCancelled(ToolContext context)
        {
            CurrentShape = null;
            MessageBus.SendMessage(new CanvasInvalidateMessage());
        }

        public virtual void DrawPreview(SKCanvas canvas, ToolContext context)
        {
            if (CurrentShape != null)
            {
                CurrentShape.Draw(canvas);
            }
        }
    }
}
