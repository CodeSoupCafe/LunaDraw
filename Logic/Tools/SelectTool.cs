using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public enum SelectionState { None, Selecting, Dragging, Resizing }
  public enum ResizeHandle { None, TopLeft, TopRight, BottomLeft, BottomRight, Top, Right, Bottom, Left }

  public class SelectTool : IDrawingTool
  {
    public string Name => "Select";
    public ToolType Type => ToolType.Select;

    private SKPoint lastPoint;
    private SelectionState currentState = SelectionState.None;
    private ResizeHandle activeHandle = ResizeHandle.None;
    private SKRect originalBounds;
    private Dictionary<IDrawableElement, SKMatrix> originalTransforms = [];
    private SKPoint resizeStartPoint;
    private readonly IMessageBus messageBus;

    public SelectTool(IMessageBus messageBus)
    {
        this.messageBus = messageBus;
    }

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      lastPoint = point;

      // Check for resize handles if we have a selection
      if (context.SelectionManager.Selected.Any())
      {
        var bounds = context.SelectionManager.GetBounds();
        var handle = GetResizeHandle(point, bounds, context.Scale);

        if (handle != ResizeHandle.None)
        {
          currentState = SelectionState.Resizing;
          activeHandle = handle;
          resizeStartPoint = point;
          originalBounds = bounds;
          originalTransforms = context.SelectionManager.GetAll()
            .ToDictionary(e => e, e => e.TransformMatrix);

          messageBus.SendMessage(new CanvasInvalidateMessage());
          return;
        }
      }

      IDrawableElement? hitElement = null;

      if (context.Layers != null)
      {
          // Iterate layers from Top (Last) to Bottom (First)
          foreach (var layer in context.Layers.Reverse())
          {
              if (!layer.IsVisible) continue;

              // Hit test elements in this layer, sorted by ZIndex Descending (Topmost first)
              var hit = layer.Elements
                             .Where(e => e.IsVisible)
                             .OrderByDescending(e => e.ZIndex)
                             .FirstOrDefault(e => e.HitTest(point));
              
              if (hit != null)
              {
                  hitElement = hit;
                  break; // Found the top-most element
              }
          }
      }
      else
      {
           // Fallback to old behavior if Layers not provided (shouldn't happen with updated context)
          hitElement = context.AllElements
                              .Where(e => e.IsVisible)
                              .OrderByDescending(e => e.ZIndex)
                              .FirstOrDefault(e => e.HitTest(point));
      }

      if (hitElement != null)
      {
        if (!context.SelectionManager.Contains(hitElement))
        {
          context.SelectionManager.Clear();
          context.SelectionManager.Add(hitElement);
        }
        currentState = SelectionState.Dragging;
      }
      else
      {
        context.SelectionManager.Clear();
        currentState = SelectionState.None;
      }

      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      switch (currentState)
      {
        case SelectionState.Dragging:
          var delta = point - lastPoint;
          foreach (var element in context.SelectionManager.GetAll())
          {
            element.Translate(delta);
          }
          lastPoint = point;
          messageBus.SendMessage(new CanvasInvalidateMessage());
          break;

        case SelectionState.Resizing:
          PerformResize(point, context);
          messageBus.SendMessage(new CanvasInvalidateMessage());
          break;
      }
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (currentState == SelectionState.Dragging || currentState == SelectionState.Resizing)
      {
        messageBus.SendMessage(new DrawingStateChangedMessage());
      }

      currentState = SelectionState.None;
      activeHandle = ResizeHandle.None;
      originalTransforms?.Clear();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchCancelled(ToolContext context)
    {
      currentState = SelectionState.None;
      activeHandle = ResizeHandle.None;
      originalTransforms?.Clear();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, ToolContext context)
    {
      if (context.SelectionManager.Selected.Any())
      {
        var bounds = context.SelectionManager.GetBounds();
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
        if (currentState != SelectionState.Resizing)
        {
          float scale = context.Scale;
          // Note: context.Scale is just TotalMatrix.ScaleX in MainViewModel logic.
          // But here we need inverse scale for drawing constant size handles? 
          // GetResizeHandle used (1/ScaleX).
          // If context.Scale is ScaleX, then handleDrawScale = 1.0f / context.Scale.
          
          float handleDrawScale = 1.0f / (Math.Abs(context.Scale) < 0.0001f ? 1.0f : context.Scale);
          
          DrawResizeHandle(canvas, new SKPoint(bounds.Left, bounds.Top), handleDrawScale);
          DrawResizeHandle(canvas, new SKPoint(bounds.Right, bounds.Top), handleDrawScale);
          DrawResizeHandle(canvas, new SKPoint(bounds.Left, bounds.Bottom), handleDrawScale);
          DrawResizeHandle(canvas, new SKPoint(bounds.Right, bounds.Bottom), handleDrawScale);
          DrawResizeHandle(canvas, new SKPoint(bounds.MidX, bounds.Top), handleDrawScale);
          DrawResizeHandle(canvas, new SKPoint(bounds.Right, bounds.MidY), handleDrawScale);
          DrawResizeHandle(canvas, new SKPoint(bounds.MidX, bounds.Bottom), handleDrawScale);
          DrawResizeHandle(canvas, new SKPoint(bounds.Left, bounds.MidY), handleDrawScale);
        }
      }
    }

    private ResizeHandle GetResizeHandle(SKPoint point, SKRect bounds, float scale)
    {
      const float baseHandleSize = 24f; // Size in screen pixels at 1:1 scale
      float scaledHandleSize = baseHandleSize / scale; // Adjust based on current zoom level
      if (IsPointNear(point, new SKPoint(bounds.Left, bounds.Top), scaledHandleSize)) return ResizeHandle.TopLeft;
      if (IsPointNear(point, new SKPoint(bounds.Right, bounds.Top), scaledHandleSize)) return ResizeHandle.TopRight;
      if (IsPointNear(point, new SKPoint(bounds.Left, bounds.Bottom), scaledHandleSize)) return ResizeHandle.BottomLeft;
      if (IsPointNear(point, new SKPoint(bounds.Right, bounds.Bottom), scaledHandleSize)) return ResizeHandle.BottomRight;
      if (IsPointNear(point, new SKPoint(bounds.MidX, bounds.Top), scaledHandleSize)) return ResizeHandle.Top;
      if (IsPointNear(point, new SKPoint(bounds.Right, bounds.MidY), scaledHandleSize)) return ResizeHandle.Right;
      if (IsPointNear(point, new SKPoint(bounds.MidX, bounds.Bottom), scaledHandleSize)) return ResizeHandle.Bottom;
      if (IsPointNear(point, new SKPoint(bounds.Left, bounds.MidY), scaledHandleSize)) return ResizeHandle.Left;
      return ResizeHandle.None;
    }

    private bool IsPointNear(SKPoint p1, SKPoint p2, float tolerance)
    {
      return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) < tolerance * tolerance;
    }

    private void PerformResize(SKPoint currentPoint, ToolContext context)
    {
      if (originalTransforms == null) return;

      var dragDelta = currentPoint - resizeStartPoint;
      var newBounds = CalculateNewBounds(originalBounds, activeHandle, dragDelta);

      if (newBounds.Width < 5 || newBounds.Height < 5) return;

      var sx = originalBounds.Width == 0 ? 1 : newBounds.Width / originalBounds.Width;
      var sy = originalBounds.Height == 0 ? 1 : newBounds.Height / originalBounds.Height;
      var tx = newBounds.Left - (originalBounds.Left * sx);
      var ty = newBounds.Top - (originalBounds.Top * sy);

      var transformFromOriginal = new SKMatrix(sx, 0, tx, 0, sy, ty, 0, 0, 1);

      foreach (var element in context.SelectionManager.GetAll())
      {
        if (originalTransforms.TryGetValue(element, out var originalMatrix))
        {
          // Apply the resize transformation (calculated in world space) AFTER the original matrix
          // New = Resize * Original
          element.TransformMatrix = SKMatrix.Concat(transformFromOriginal, originalMatrix);
        }
      }
    }

    private SKRect CalculateNewBounds(SKRect bounds, ResizeHandle handle, SKPoint dragDelta)
    {
      float left = bounds.Left, top = bounds.Top, right = bounds.Right, bottom = bounds.Bottom;

      switch (handle)
      {
        case ResizeHandle.TopLeft: left += dragDelta.X; top += dragDelta.Y; break;
        case ResizeHandle.Top: top += dragDelta.Y; break;
        case ResizeHandle.TopRight: right += dragDelta.X; top += dragDelta.Y; break;
        case ResizeHandle.Left: left += dragDelta.X; break;
        case ResizeHandle.Right: right += dragDelta.X; break;
        case ResizeHandle.BottomLeft: left += dragDelta.X; bottom += dragDelta.Y; break;
        case ResizeHandle.Bottom: bottom += dragDelta.Y; break;
        case ResizeHandle.BottomRight: right += dragDelta.X; bottom += dragDelta.Y; break;
      }

      if (left > right) { var temp = left; left = right; right = temp; }
      if (top > bottom) { var temp = top; top = bottom; bottom = temp; }

      return new SKRect(left, top, right, bottom);
    }

    private void DrawResizeHandle(SKCanvas canvas, SKPoint point, float scale)
    {
      const float baseHandleSize = 4f;
      float handleSize = baseHandleSize * scale;
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