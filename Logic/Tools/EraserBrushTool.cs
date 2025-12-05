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

        // Determine if this is a "pure stroke" (like a freehand line or line shape) vs a "filled shape"
        // A DrawablePath is a pure stroke if IsFilled is false.
        // A DrawableLine is always a stroke.
        // A shape (like Rect/Ellipse) with no FillColor is visually just a stroke.
        bool isPureStroke = (element is DrawablePath dp && !dp.IsFilled) || 
                            (element is DrawableLine) ||
                            (element.FillColor == null);

        SKPath elementPath;
        if (isPureStroke)
        {
            // OLD BEHAVIOR for strokes: Get the visual outline
            elementPath = element.GetPath();
        }
        else
        {
            // NEW BEHAVIOR for shapes: Get the geometry contour
            elementPath = element.GetGeometryPath();
        }

        using (elementPath)
        {
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
                    TransformMatrix = SKMatrix.CreateIdentity(),
                    IsVisible = element.IsVisible,
                    Opacity = element.Opacity,
                    ZIndex = element.ZIndex,
                    IsSelected = element.IsSelected,
                    IsGlowEnabled = element.IsGlowEnabled,
                    GlowColor = element.GlowColor,
                    GlowRadius = element.GlowRadius
                  };

                  if (isPureStroke)
                  {
                      // Result of eroding a stroke is a filled shape (the leftover pieces of the outline)
                      newElement.StrokeWidth = 0;
                      newElement.IsFilled = true;
                      newElement.StrokeColor = element.StrokeColor; // Use original stroke color as the "fill" color of the blob
                      newElement.FillColor = null; // Ensure logic picks up StrokeColor
                  }
                  else
                  {
                      // Result of eroding a shape preserves shape properties
                      newElement.StrokeWidth = element.StrokeWidth;
                      newElement.StrokeColor = element.StrokeColor;
                      newElement.FillColor = element.FillColor;
                      
                      // Determine IsFilled state
                      // If it was a shape (Rect/Ellipse), it effectively has area, so IsFilled=true.
                      // If it was a filled path, IsFilled=true.
                      // If it was a stroke with fill (DrawablePath with IsFilled=true), preserve it.
                      // Generally, if we used GetGeometryPath (which implies area logic), IsFilled should be true for the resulting path to show up as a shape.
                      newElement.IsFilled = true; 
                  }

                  if (element is DrawablePath originalPath)
                  {
                    newElement.BlendMode = originalPath.BlendMode;
                  }

                  elementsToRemove.Add(element);
                  elementsToAdd.Add(newElement);
                }
                modified = true;
              }
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