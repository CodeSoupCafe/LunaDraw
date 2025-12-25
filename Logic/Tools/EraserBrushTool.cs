/* 
 *  Copyright (c) 2025 CodeSoupCafe LLC
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 *  
 */

using LunaDraw.Logic.Extensions;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using ReactiveUI;

using SkiaSharp;
using LunaDraw.Logic.Storage;

namespace LunaDraw.Logic.Tools;

public class EraserBrushTool(IMessageBus messageBus, IPreferencesFacade preferencesFacade) : IDrawingTool
{
  public string Name => "Eraser";
  public ToolType Type => ToolType.Eraser;

  private SKPath? currentPath;
  private DrawablePath? currentDrawablePath;
  private readonly IMessageBus messageBus = messageBus;
  private readonly IPreferencesFacade preferencesFacade = preferencesFacade;

  public void OnTouchPressed(SKPoint point, ToolContext context)
  {
    if (context.CurrentLayer?.IsLocked == true) return;

    currentPath = new SKPath();
    currentPath.MoveTo(point);

    currentDrawablePath = new DrawablePath
    {
      Path = currentPath,
      StrokeColor = preferencesFacade.GetCanvasBackgroundColor(), // Visual preview color
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
      bool isPureStroke;
      if (element is DrawablePath dp)
      {
        // A DrawablePath is a pure stroke only if it is explicitly NOT filled.
        // If it has a FillShader (e.g. erased image) or IsFilled=true, it is a shape.
        isPureStroke = !dp.IsFilled;
      }
      else if (element is DrawableLine)
      {
        isPureStroke = true;
      }
      else if (element is DrawableImage)
      {
        isPureStroke = false;
      }
      else
      {
        // For other shapes (Rect, Ellipse), they are pure strokes if they have no fill.
        isPureStroke = element.FillColor == null;
      }

      if (element is DrawableStamps stamps)
      {
        var remainingPoints = new List<SKPoint>();
        var remainingRotations = new List<float>();
        var stampModified = false;

        // Iterate through detailed instances to handle geometry & color accurately
        var instances = stamps.GetDetailedPaths().ToList();

        // We need to match instances back to original points by index
        for (int i = 0; i < instances.Count; i++)
        {
          var (stampPath, stampColor) = instances[i];
          var originalPoint = stamps.Points[i];

          using (stampPath)
          {
            // Check intersection
            using var intersection = new SKPath();
            bool intersects = eraserOutline.Op(stampPath, SKPathOp.Intersect, intersection) && !intersection.IsEmpty;

            if (!intersects)
            {
              // Completely untouched, keep as a stamp
              remainingPoints.Add(originalPoint);
              if (stamps.Rotations != null && i < stamps.Rotations.Count)
              {
                remainingRotations.Add(stamps.Rotations[i]);
              }
            }
            else
            {
              // Touched (Partial or Full erase) -> Convert to Path(s) or Destroy
              stampModified = true;

              using var resultPath = new SKPath();
              if (stampPath.Op(eraserOutline, SKPathOp.Difference, resultPath) && !resultPath.IsEmpty)
              {
                // Create a new DrawablePath for the fragment
                var newFragment = new DrawablePath
                {
                  Path = new SKPath(resultPath), // Copy the result
                  TransformMatrix = SKMatrix.CreateIdentity(), // Logic was already applied in GetDetailedPaths (including TransformMatrix)
                  IsVisible = stamps.IsVisible,
                  Opacity = (byte)(stamps.Opacity * stamps.Flow / 255f), // Combine Opacity and Flow
                  ZIndex = stamps.ZIndex,
                  IsSelected = false, // Fragments shouldn't inherit selection immediately
                  IsGlowEnabled = stamps.IsGlowEnabled,
                  GlowColor = stamps.GlowColor,
                  GlowRadius = stamps.GlowRadius,
                  IsFilled = true, // Stamps are filled shapes
                  StrokeWidth = 0,
                  StrokeColor = stampColor, // Use the specific jittered color
                  FillColor = null, // Logic implies "Filled Stroke" behavior for consistency with other paths
                  BlendMode = stamps.BlendMode
                };

                // Note: 'stampColor' is used as StrokeColor with IsFilled=true because 
                // in the generic path logic above (lines 169-172), eroded strokes become filled blobs 
                // where StrokeColor is preserved. DrawableStamps usually render as fills of the 'StrokeColor'.

                elementsToAdd.Add(newFragment);
              }
              // If resultPath is empty, it was fully erased. Do nothing (it's gone).
            }
          }
        }

        if (stampModified)
        {
          if (remainingPoints.Count == 0)
          {
            elementsToRemove.Add(element);
          }
          else
          {
            // Create a new Stamps object for the remaining untouched stamps
            var newStamps = (DrawableStamps)stamps.Clone();
            newStamps.Points = remainingPoints;
            newStamps.Rotations = remainingRotations;

            elementsToRemove.Add(element);
            elementsToAdd.Add(newStamps);
          }
          modified = true;
        }
        continue;
      }

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
                TransformMatrix = SKMatrix.CreateIdentity(), // We might need to adjust this depending on how GetGeometryPath works
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
                newElement.StrokeColor = element.StrokeColor;
                newElement.FillColor = null;
              }
              else
              {
                // Result of eroding a shape preserves shape properties
                newElement.StrokeWidth = element.StrokeWidth;
                newElement.StrokeColor = element.StrokeColor;
                newElement.FillColor = element.FillColor;
                newElement.IsFilled = true;

                // TRANSFORM FIX:
                // Put the path back into the original element's coordinate space
                if (element.TransformMatrix.TryInvert(out var inverseMatrix))
                {
                  newElement.Path.Transform(inverseMatrix);
                  newElement.TransformMatrix = element.TransformMatrix;
                }

                // Handle Image Shader creation specifically here
                if (element is DrawableImage iamge)
                {
                  // Since we reverted to Local Space, the shader is simple (Identity matrix)
                  var shader = SKShader.CreateBitmap(iamge.Bitmap, SKShaderTileMode.Decal, SKShaderTileMode.Decal);
                  newElement.FillShader = shader;
                  newElement.IsFilled = true;

                  // Ensure we carry over opacity and stuff
                  newElement.Opacity = iamge.Opacity;
                }
                else if (element is DrawablePath oldPath)
                {
                  newElement.FillShader = oldPath.FillShader;
                }
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
      var finalElements = new List<IDrawableElement>();

      // Add all elements that were NOT removed
      foreach (var element in elements) // 'elements' is a copy from the start of the method
      {
        if (!elementsToRemove.Contains(element))
        {
          finalElements.Add(element);
        }
      }

      // Add all newly created elements (fragments from erased items)
      finalElements.AddRange(elementsToAdd);

      // Clear the ObservableCollection once, then re-populate it
      context.CurrentLayer.Elements.Clear();
      foreach (var item in finalElements.OrderBy(e => e.ZIndex)) // Add in sorted ZIndex order
      {
        context.CurrentLayer.Elements.Add(item);
      }

      // Normalize Z-indices on the actual collection elements *after* they are in the collection
      var currentLayerElementsInCollection = context.CurrentLayer.Elements.OrderBy(e => e.ZIndex).ToList();
      for (int i = 0; i < currentLayerElementsInCollection.Count; i++)
      {
        currentLayerElementsInCollection[i].ZIndex = i;
      }

      messageBus.SendMessage(new DrawingStateChangedMessage());
      messageBus.SendMessage(new CanvasInvalidateMessage());
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

  public void DrawPreview(SKCanvas canvas, ToolContext context)
  {
    // Optional: Draw a circle cursor for eraser size
  }
}