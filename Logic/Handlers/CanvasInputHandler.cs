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

using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.Services;

public class CanvasInputHandler(
    ToolbarViewModel toolbarViewModel,
    ILayerFacade layerFacade,
    SelectionObserver selectionObserver,
    NavigationModel navigationModel,
    IMessageBus messageBus) : ICanvasInputHandler
{
  private readonly ToolbarViewModel toolbarViewModel = toolbarViewModel;
  private readonly ILayerFacade layerFacade = layerFacade;
  private readonly SelectionObserver selectionObserver = selectionObserver;
  private readonly NavigationModel navigationModel = navigationModel;
  private readonly IMessageBus messageBus = messageBus;

  private readonly Dictionary<long, SKPoint> activeTouches = [];
  private bool isMultiTouch = false;
  private bool manipulatingSelection = false;

  // Gesture state
  private SKPoint startCentroid;
  private float startDistance;
  private float startAngle;
  private SKMatrix startMatrix;
  private Dictionary<IDrawableElement, SKMatrix> startElementMatrices = [];

  // Smoothing
  private SKMatrix previousOutputMatrix = SKMatrix.CreateIdentity();
  private Dictionary<IDrawableElement, SKMatrix> previousElementMatrices = [];
  private const float SmoothingFactor = 0.1f; // Lower = more smoothing (0.3 was original)

  public void ProcessTouch(SKTouchEventArgs e, SKRect canvasViewPort)
  {
    if (layerFacade.CurrentLayer == null) return;

    var location = e.Location;

    // Right click = select
    if (e.MouseButton == SKMouseButton.Right)
    {
      if (e.ActionType == SKTouchAction.Pressed)
      {
        var selectTool = toolbarViewModel.AvailableTools.FirstOrDefault(t => t.Type == ToolType.Select);
        if (selectTool != null) toolbarViewModel.ActiveTool = selectTool;

        if (navigationModel.ViewMatrix.TryInvert(out var inverse))
        {
          PerformContextSelection(inverse.MapPoint(location));
        }
      }
      return;
    }

    // Track touches
    switch (e.ActionType)
    {
      case SKTouchAction.Pressed:
        activeTouches[e.Id] = location;
        break;
      case SKTouchAction.Released:
      case SKTouchAction.Cancelled:
        activeTouches.Remove(e.Id);
        break;
      case SKTouchAction.Moved:
        activeTouches[e.Id] = location;
        break;
    }

    // Multi-touch state management
    if (activeTouches.Count >= 2)
    {
      if (!isMultiTouch)
      {
        isMultiTouch = true;

        // Cancel drawing
        if (toolbarViewModel.ActiveTool is IDrawingTool tool)
        {
          tool.OnTouchCancelled(CreateToolContext());
        }

        // Snapshot state
        var touches = activeTouches.OrderBy(kvp => kvp.Key).Take(2).Select(kvp => kvp.Value).ToArray();
        startCentroid = new SKPoint((touches[0].X + touches[1].X) / 2f, (touches[0].Y + touches[1].Y) / 2f);
        startDistance = Distance(touches[0], touches[1]);
        startAngle = (float)Math.Atan2(touches[1].Y - touches[0].Y, touches[1].X - touches[0].X);
        startMatrix = navigationModel.ViewMatrix;
        previousOutputMatrix = navigationModel.ViewMatrix;

        // Check if manipulating selection
        manipulatingSelection = false;
        if (layerFacade.CurrentLayer?.IsLocked == false && selectionObserver.Selected.Any())
        {
          if (navigationModel.ViewMatrix.TryInvert(out var inv))
          {
            foreach (var touch in activeTouches.Values)
            {
              var worldPt = inv.MapPoint(touch);
              if (selectionObserver.Selected.Any(el => el.HitTest(worldPt)))
              {
                manipulatingSelection = true;
                startElementMatrices = selectionObserver.Selected.ToDictionary(el => el, el => el.TransformMatrix);
                break;
              }
            }
          }
        }
      }

      // Handle multi-touch ONCE after all touch updates
      if (e.ActionType == SKTouchAction.Moved)
      {
        HandleMultiTouch();
      }
    }
    else
    {
      isMultiTouch = false;
      manipulatingSelection = false;
      startElementMatrices.Clear();
      previousOutputMatrix = SKMatrix.CreateIdentity();

      // Single touch
      if (navigationModel.ViewMatrix.TryInvert(out var inverse))
      {
        var worldPoint = inverse.MapPoint(location);
        var context = CreateToolContext();

        switch (e.ActionType)
        {
          case SKTouchAction.Pressed:
            HandleTouchPressed(worldPoint, context);
            break;
          case SKTouchAction.Moved:
            toolbarViewModel.ActiveTool.OnTouchMoved(worldPoint, context);
            break;
          case SKTouchAction.Released:
            toolbarViewModel.ActiveTool.OnTouchReleased(worldPoint, context);
            break;
        }
      }
    }
  }

  private void HandleMultiTouch()
  {
    var touches = activeTouches.OrderBy(kvp => kvp.Key).Take(2).Select(kvp => kvp.Value).ToArray();
    if (touches.Length < 2) return;

    // Current centroid (average of both fingers)
    var centroid = new SKPoint((touches[0].X + touches[1].X) / 2f, (touches[0].Y + touches[1].Y) / 2f);

    // Calculate current gesture state
    float distance = Distance(touches[0], touches[1]);
    float angle = (float)Math.Atan2(touches[1].Y - touches[0].Y, touches[1].X - touches[0].X);

    // Calculate transform from start
    var translation = centroid - startCentroid;
    float scale = startDistance > 0.001f ? distance / startDistance : 1.0f;
    float rotation = angle - startAngle;

    // Aggressive deadzones to filter out noise
    if (Math.Abs(scale - 1.0f) < 0.05f) scale = 1.0f;
    if (Math.Abs(rotation) < 0.2f) rotation = 0f; // ~11 degrees

    // Build transform around start centroid
    var transform = SKMatrix.CreateIdentity();
    transform = transform.PostConcat(SKMatrix.CreateTranslation(-startCentroid.X, -startCentroid.Y));
    transform = transform.PostConcat(SKMatrix.CreateScale(scale, scale));
    transform = transform.PostConcat(SKMatrix.CreateRotation(rotation));
    transform = transform.PostConcat(SKMatrix.CreateTranslation(startCentroid.X, startCentroid.Y));
    transform = transform.PostConcat(SKMatrix.CreateTranslation(translation.X, translation.Y));

    if (manipulatingSelection)
    {
      if (navigationModel.ViewMatrix.TryInvert(out var invView))
      {
        var worldTransform = SKMatrix.Concat(invView, SKMatrix.Concat(transform, navigationModel.ViewMatrix));

        foreach (var element in selectionObserver.Selected)
        {
          if (startElementMatrices.TryGetValue(element, out var startMat))
          {
            var elementTarget = SKMatrix.Concat(worldTransform, startMat);

            // Smooth element transforms too
            if (!previousElementMatrices.ContainsKey(element))
            {
              previousElementMatrices[element] = element.TransformMatrix;
            }

            var smoothedElementMatrix = LerpMatrix(previousElementMatrices[element], elementTarget, SmoothingFactor);
            element.TransformMatrix = smoothedElementMatrix;
            previousElementMatrices[element] = smoothedElementMatrix;
          }
        }
      }
    }
    else
    {
      // Calculate target matrix
      var targetMatrix = SKMatrix.Concat(transform, startMatrix);

      // Smooth the output using exponential moving average
      var smoothedMatrix = LerpMatrix(previousOutputMatrix, targetMatrix, SmoothingFactor);
      previousOutputMatrix = smoothedMatrix;

      navigationModel.ViewMatrix = smoothedMatrix;
    }

    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private SKMatrix LerpMatrix(SKMatrix a, SKMatrix b, float t)
  {
    return new SKMatrix
    {
      ScaleX = a.ScaleX + (b.ScaleX - a.ScaleX) * t,
      ScaleY = a.ScaleY + (b.ScaleY - a.ScaleY) * t,
      SkewX = a.SkewX + (b.SkewX - a.SkewX) * t,
      SkewY = a.SkewY + (b.SkewY - a.SkewY) * t,
      TransX = a.TransX + (b.TransX - a.TransX) * t,
      TransY = a.TransY + (b.TransY - a.TransY) * t,
      Persp0 = a.Persp0 + (b.Persp0 - a.Persp0) * t,
      Persp1 = a.Persp1 + (b.Persp1 - a.Persp1) * t,
      Persp2 = a.Persp2 + (b.Persp2 - a.Persp2) * t
    };
  }

  private void PerformContextSelection(SKPoint worldPoint)
  {
    IDrawableElement? hit = null;
    Layer? hitLayer = null;

    foreach (var layer in layerFacade.Layers.Reverse())
    {
      if (!layer.IsVisible || layer.IsLocked) continue;

      hit = layer.Elements
        .Where(e => e.IsVisible)
        .OrderByDescending(e => e.ZIndex)
        .FirstOrDefault(e => e.HitTest(worldPoint));

      if (hit != null)
      {
        hitLayer = layer;
        break;
      }
    }

    if (hit != null)
    {
      if (!selectionObserver.Contains(hit))
      {
        selectionObserver.Clear();
        selectionObserver.Add(hit);
      }
      if (hitLayer != null) layerFacade.CurrentLayer = hitLayer;
    }
    else
    {
      selectionObserver.Clear();
    }

    messageBus.SendMessage(new CanvasInvalidateMessage());
  }

  private void HandleTouchPressed(SKPoint worldPoint, ToolContext context)
  {
    if (layerFacade.CurrentLayer?.IsLocked == true) return;

    if (toolbarViewModel.ActiveTool.Type == ToolType.Select)
    {
      toolbarViewModel.ActiveTool.OnTouchPressed(worldPoint, context);

      if (selectionObserver.Selected.Count > 0)
      {
        var layer = layerFacade.Layers.FirstOrDefault(l => l.Elements.Contains(selectionObserver.Selected[0]));
        if (layer != null && layer != layerFacade.CurrentLayer)
        {
          layerFacade.CurrentLayer = layer;
        }
      }
      return;
    }

    if (selectionObserver.Selected.Any())
    {
      selectionObserver.Clear();
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    toolbarViewModel.ActiveTool.OnTouchPressed(worldPoint, context);
  }

  private float Distance(SKPoint p1, SKPoint p2) =>
    (float)Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));

  private ToolContext CreateToolContext()
  {
    return new ToolContext
    {
      CurrentLayer = layerFacade.CurrentLayer!,
      StrokeColor = toolbarViewModel.StrokeColor,
      FillColor = toolbarViewModel.FillColor,
      StrokeWidth = toolbarViewModel.StrokeWidth,
      Opacity = toolbarViewModel.Opacity,
      Flow = toolbarViewModel.Flow,
      Spacing = toolbarViewModel.Spacing,
      BrushShape = toolbarViewModel.CurrentBrushShape,
      AllElements = layerFacade.Layers.SelectMany(l => l.Elements),
      Layers = layerFacade.Layers,
      SelectionObserver = selectionObserver,
      Scale = navigationModel.ViewMatrix.ScaleX,
      IsGlowEnabled = toolbarViewModel.IsGlowEnabled,
      GlowColor = toolbarViewModel.GlowColor,
      GlowRadius = toolbarViewModel.GlowRadius,
      IsRainbowEnabled = toolbarViewModel.IsRainbowEnabled,
      ScatterRadius = toolbarViewModel.ScatterRadius,
      SizeJitter = toolbarViewModel.SizeJitter,
      AngleJitter = toolbarViewModel.AngleJitter,
      HueJitter = toolbarViewModel.HueJitter,
      CanvasMatrix = navigationModel.ViewMatrix
    };
  }
}