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

    private SKPath? currentPath;
    private DrawablePath? currentDrawablePath;
    private readonly IMessageBus messageBus;

    public EraserBrushTool(IMessageBus messageBus)
    {
        this.messageBus = messageBus;
    }

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      currentPath = new SKPath();
      currentPath.MoveTo(point);

      currentDrawablePath = new DrawablePath
      {
        Path = currentPath,
        StrokeColor = SKColors.White, // Visual preview color
        StrokeWidth = context.StrokeWidth * 2, // Eraser usually wider
        Opacity = 255,
        BlendMode = SKBlendMode.SrcOver,
        ZIndex = context.CurrentLayer?.Elements.Count > 0 ? context.CurrentLayer.Elements.Max(e => e.ZIndex) + 1 : 0
      };

      context.CurrentLayer?.Elements.Add(currentDrawablePath);
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (currentPath == null || context.CurrentLayer?.IsLocked == true) return;

      currentPath.LineTo(point);
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (currentPath == null || context.CurrentLayer == null) return;

      currentPath.LineTo(point);

      // Remove the temporary "global" eraser path
      if (currentDrawablePath != null)
      {
        context.CurrentLayer.Elements.Remove(currentDrawablePath);
      }

      // Convert stroke to fill path (outline) for operations
      using var strokePaint = new SKPaint
      {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = context.StrokeWidth * 2,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round
      };
      using var eraserOutline = new SKPath();
      strokePaint.GetFillPath(currentPath, eraserOutline);

      var elements = context.CurrentLayer.Elements.ToList();
      var modified = false;
      var elementsToRemove = new List<IDrawableElement>();
      var elementsToAdd = new List<IDrawableElement>();

      foreach (var element in elements)
      {
        if (element == currentDrawablePath) continue;
        if (!element.IsVisible) continue;

        // Get element geometry
        // Note: GetPath() returns the fill-path (outline) for stroked elements
        using var elementPath = element.GetPath();

        // Check for intersection first (optimization)
        using var intersection = new SKPath();
        if (eraserOutline.Op(elementPath, SKPathOp.Intersect, intersection) && !intersection.IsEmpty)
        {
          // Calculate the difference (Element - Eraser)
          var resultPath = new SKPath();
          if (elementPath.Op(eraserOutline, SKPathOp.Difference, resultPath))
          {
            if (resultPath.IsEmpty)
            {
              // Element completely erased
              elementsToRemove.Add(element);
            }
            else
            {
              // Create new element with the remaining geometry
              var newElement = new DrawablePath
              {
                Path = resultPath,
                StrokeWidth = 0,
                IsFilled = true,
                BlendMode = SKBlendMode.SrcOver,
                Opacity = element.Opacity,
                ZIndex = element.ZIndex,
                IsSelected = element.IsSelected
              };

              // Try to get a better color match
              SKColor finalColor = SKColors.Black;
              if (element is DrawablePath p)
              {
                // If the source was filled, use its fill-driving color (which is StrokeColor in this codebase).
                // If it was a stroke, use its StrokeColor.
                finalColor = p.StrokeColor;
              }
              else if (element.GetType().GetProperty("StrokeColor")?.GetValue(element) is SKColor sc)
              {
                finalColor = sc;
              }
              else if (element.GetType().GetProperty("FillColor")?.GetValue(element) is SKColor fc)
              {
                finalColor = fc;
              }

              // DrawablePath uses StrokeColor property for drawing, even when IsFilled is true.
              newElement.StrokeColor = finalColor;

              elementsToRemove.Add(element);
              elementsToAdd.Add(newElement);
            }
            modified = true;
          }
        }
      }

      if (modified)
      {
        foreach (var item in elementsToRemove)
        {
          context.CurrentLayer.Elements.Remove(item);
        }
        foreach (var item in elementsToAdd)
        {
          context.CurrentLayer.Elements.Add(item);
        }

        messageBus.SendMessage(new DrawingStateChangedMessage());
      }
      messageBus.SendMessage(new CanvasInvalidateMessage());

      currentPath = null;
      currentDrawablePath = null;
    }

    public void OnTouchCancelled(ToolContext context)
    {
      if (currentDrawablePath != null && context.CurrentLayer != null)
      {
        context.CurrentLayer.Elements.Remove(currentDrawablePath);
      }

      currentPath = null;
      currentDrawablePath = null;
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
      // Optional: Draw a circle cursor for eraser size
    }
  }
}