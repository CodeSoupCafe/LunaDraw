using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.Services
{
  public class CanvasInputHandler(
      IToolStateManager toolStateManager,
      ILayerStateManager layerStateManager,
      SelectionManager selectionManager,
      NavigationModel navigationModel,
      IMessageBus messageBus) : ICanvasInputHandler
  {
    private readonly IToolStateManager toolStateManager = toolStateManager;
    private readonly ILayerStateManager layerStateManager = layerStateManager;
    private readonly SelectionManager selectionManager = selectionManager;
    private readonly NavigationModel navigationModel = navigationModel;
    private readonly IMessageBus messageBus = messageBus;

    private readonly Dictionary<long, SKPoint> activeTouches = [];
    private bool isMultiTouching = false;
    private bool isManipulatingSelection = false;

    // Multi-touch gesture state (snapshot at gesture start)
    private SKPoint gestureStartCentroid;
    private float gestureStartDistance;
    private float gestureStartAngle;
    private SKPoint previousCentroid;
    private float previousDistance;
    private float previousAngle;

    // Movement deadzone threshold to reduce jitter on tiny movements
    private const float MovementThreshold = 2.0f;
    private const float ScaleThreshold = 0.001f;
    private const float RotationThreshold = 0.01f; // radians

    public void ProcessTouch(SKTouchEventArgs e, SKRect canvasViewPort)
    {
      if (layerStateManager.CurrentLayer == null) return;

      // e.Location is already relative to the SKCanvasView (pixel coordinates)
      var adjustedLocation = e.Location;

      // Handle Right Click for Context Selection
      if (e.MouseButton == SKMouseButton.Right)
      {
        if (e.ActionType == SKTouchAction.Pressed)
        {
          // Switch to Select Tool
          var selectTool = toolStateManager.AvailableTools.FirstOrDefault(t => t.Type == ToolType.Select);
          if (selectTool != null)
          {
            toolStateManager.ActiveTool = selectTool;
          }

          SKMatrix inverse = SKMatrix.CreateIdentity();
          if (navigationModel.TotalMatrix.TryInvert(out inverse))
          {
            var worldPoint = inverse.MapPoint(adjustedLocation);
            PerformContextSelection(worldPoint);
          }
        }
        return; // Stop processing to prevent drawing/dragging
      }

      switch (e.ActionType)
      {
        case SKTouchAction.Pressed:
          activeTouches[e.Id] = adjustedLocation;
          break;
        case SKTouchAction.Released:
        case SKTouchAction.Cancelled:
          activeTouches.Remove(e.Id);
          break;
      }

      // Handle Multi-Touch State Transition
      if (activeTouches.Count >= 2)
      {
        if (!isMultiTouching)
        {
          isMultiTouching = true;

          // Cancel any active drawing tool
          if (toolStateManager.ActiveTool is IDrawingTool tool)
          {
            var context = CreateToolContext();
            tool.OnTouchCancelled(context);
          }

          // Determine manipulation target (View vs Selection) ONCE at the start of multi-touch
          DetermineMultiTouchTarget();

          // Initialize gesture snapshot
          InitializeGestureSnapshot();
        }
      }
      else
      {
        isMultiTouching = false;
        isManipulatingSelection = false;
      }

      // Handle Navigation (Multi-touch)
      if (activeTouches.Count >= 2 && e.ActionType == SKTouchAction.Moved && activeTouches.ContainsKey(e.Id))
      {
        HandleMultiTouch(adjustedLocation, e.Id);
        return;
      }

      // Handle Drawing/Tools (Single touch)
      if (activeTouches.Count <= 1)
      {
        HandleSingleTouch(adjustedLocation, e.ActionType);
      }

      // Update stored location for Moved events if not handled by navigation
      if (e.ActionType == SKTouchAction.Moved && activeTouches.ContainsKey(e.Id))
      {
        activeTouches[e.Id] = adjustedLocation;
      }
    }

    private void PerformContextSelection(SKPoint point)
    {
      IDrawableElement? hitElement = null;
      Layer? hitLayer = null;

      // Iterate layers from Top (Last) to Bottom (First)
      foreach (var layer in layerStateManager.Layers.Reverse())
      {
        if (!layer.IsVisible || layer.IsLocked) continue;

        var hit = layer.Elements
                       .Where(e => e.IsVisible)
                       .OrderByDescending(e => e.ZIndex)
                       .FirstOrDefault(e => e.HitTest(point));

        if (hit != null)
        {
          hitElement = hit;
          hitLayer = layer;
          break;
        }
      }

      if (hitElement != null)
      {
        if (!selectionManager.Contains(hitElement))
        {
          selectionManager.Clear();
          selectionManager.Add(hitElement);
        }
        if (hitLayer != null)
        {
          layerStateManager.CurrentLayer = hitLayer;
        }
      }
      else
      {
        selectionManager.Clear();
      }
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    private void InitializeGestureSnapshot()
    {
      // Get the two primary fingers
      var sortedKeys = activeTouches.Keys.OrderBy(k => k).ToList();
      if (sortedKeys.Count < 2) return;

      long id1 = sortedKeys[0];
      long id2 = sortedKeys[1];

      SKPoint p1 = activeTouches[id1];
      SKPoint p2 = activeTouches[id2];

      // Store initial gesture state
      gestureStartCentroid = CalculateCentroid(p1, p2);
      gestureStartDistance = Distance(p1, p2);
      gestureStartAngle = CalculateAngle(p1, p2);

      // Initialize "previous" values to current state
      previousCentroid = gestureStartCentroid;
      previousDistance = gestureStartDistance;
      previousAngle = gestureStartAngle;
    }

    private void DetermineMultiTouchTarget()
    {
      isManipulatingSelection = false;
      var selectedElements = selectionManager.Selected;

      if (layerStateManager.CurrentLayer?.IsLocked == false && selectedElements.Any())
      {
        SKMatrix inverseView;
        bool canInvert = navigationModel.TotalMatrix.TryInvert(out inverseView);

        if (canInvert)
        {
          // Check if ANY active touch is on a selected element
          foreach (var touchPoint in activeTouches.Values)
          {
            var worldPoint = inverseView.MapPoint(touchPoint);
            if (selectedElements.Any(el => el.HitTest(worldPoint)))
            {
              isManipulatingSelection = true;
              return;
            }
          }
        }
      }
    }

    private void HandleMultiTouch(SKPoint newLocation, long id)
    {
      // 1. Identify the two primary fingers
      var sortedKeys = activeTouches.Keys.OrderBy(k => k).ToList();
      if (sortedKeys.Count < 2) return;

      long id1 = sortedKeys[0];
      long id2 = sortedKeys[1];

      // 2. Get CURRENT positions BEFORE updating dictionary
      // One finger is at its old position (in dictionary), the other just moved
      SKPoint currentP1 = (id == id1) ? newLocation : activeTouches[id1];
      SKPoint currentP2 = (id == id2) ? newLocation : activeTouches[id2];

      // 3. Calculate current gesture state
      SKPoint currentCentroid = CalculateCentroid(currentP1, currentP2);
      float currentDistance = Distance(currentP1, currentP2);
      float currentAngle = CalculateAngle(currentP1, currentP2);

      // 4. Calculate deltas from PREVIOUS frame (not from gesture start)
      SKPoint centroidDelta = currentCentroid - previousCentroid;
      float scaleDelta = (previousDistance > 0.001f) ? currentDistance / previousDistance : 1.0f;
      float rotationDelta = currentAngle - previousAngle;

      // 5. Apply thresholds to reduce jitter on tiny movements
      bool shouldTransform = false;

      if (Math.Abs(centroidDelta.X) > MovementThreshold || Math.Abs(centroidDelta.Y) > MovementThreshold)
      {
        shouldTransform = true;
      }

      if (Math.Abs(scaleDelta - 1.0f) > ScaleThreshold)
      {
        shouldTransform = true;
      }

      if (Math.Abs(rotationDelta) > RotationThreshold)
      {
        shouldTransform = true;
      }

      // 6. Build transformation matrix if movement exceeds threshold
      if (shouldTransform)
      {
        // Use PREVIOUS centroid as the pivot point for transformation
        SKMatrix matrix = BuildTransformationMatrix(
          previousCentroid,
          scaleDelta,
          rotationDelta,
          centroidDelta
        );

        // Safety check for invalid matrix values
        if (!float.IsNaN(matrix.ScaleX) && !float.IsInfinity(matrix.ScaleX))
        {
          // 7. Apply to Model
          if (isManipulatingSelection)
          {
            ApplySelectionTransform(matrix);
          }
          else
          {
            // Apply to UserMatrix (View transformation)
            navigationModel.UserMatrix = SKMatrix.Concat(matrix, navigationModel.UserMatrix);
          }

          messageBus.SendMessage(new CanvasInvalidateMessage());
        }

        // 8. Update previous state for next frame
        previousCentroid = currentCentroid;
        previousDistance = currentDistance;
        previousAngle = currentAngle;
      }

      // 9. Update the touch dictionary for the finger that moved
      activeTouches[id] = newLocation;
    }

    private SKMatrix BuildTransformationMatrix(
      SKPoint pivot,
      float scale,
      float rotation,
      SKPoint translation)
    {
      SKMatrix matrix = SKMatrix.CreateIdentity();

      // Step 1: Translate pivot to origin
      matrix = matrix.PostConcat(SKMatrix.CreateTranslation(-pivot.X, -pivot.Y));

      // Step 2: Apply scale around origin
      matrix = matrix.PostConcat(SKMatrix.CreateScale(scale, scale));

      // Step 3: Apply rotation around origin
      matrix = matrix.PostConcat(SKMatrix.CreateRotation(rotation));

      // Step 4: Translate pivot back
      matrix = matrix.PostConcat(SKMatrix.CreateTranslation(pivot.X, pivot.Y));

      // Step 5: Apply pan/translation
      matrix = matrix.PostConcat(SKMatrix.CreateTranslation(translation.X, translation.Y));

      return matrix;
    }

    private SKPoint CalculateCentroid(SKPoint p1, SKPoint p2)
    {
      return new SKPoint((p1.X + p2.X) / 2.0f, (p1.Y + p2.Y) / 2.0f);
    }

    private float CalculateAngle(SKPoint p1, SKPoint p2)
    {
      return (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
    }

    private void ApplySelectionTransform(SKMatrix touchDelta)
    {
      var selectedElements = selectionManager.Selected;
      if (layerStateManager.CurrentLayer?.IsLocked == false && selectedElements.Any())
      {
        SKMatrix inverseView;
        SKMatrix currentTotal = navigationModel.TotalMatrix;
        if (currentTotal.TryInvert(out inverseView))
        {
          // Convert screen-space delta to world-space delta
          // DeltaWorld = View^-1 * DeltaScreen * View
          var worldDelta = SKMatrix.Concat(inverseView, SKMatrix.Concat(touchDelta, currentTotal));

          foreach (var element in selectedElements)
          {
            element.TransformMatrix = SKMatrix.Concat(worldDelta, element.TransformMatrix);
          }
        }
      }
    }

    private float Distance(SKPoint p1, SKPoint p2) => (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

    private void HandleSingleTouch(SKPoint location, SKTouchAction actionType)
    {
      // Transform point to World Coordinates using the TotalMatrix (which includes FitToScreen + User transforms)
      SKMatrix inverse = SKMatrix.CreateIdentity();
      bool canInvert = navigationModel.TotalMatrix.TryInvert(out inverse);

      if (canInvert)
      {
        var worldPoint = inverse.MapPoint(location);

        var context = CreateToolContext();

        switch (actionType)
        {
          case SKTouchAction.Pressed:
            HandleTouchPressed(worldPoint, context);
            break;
          case SKTouchAction.Moved:
            toolStateManager.ActiveTool.OnTouchMoved(worldPoint, context);
            // No need to update activeTouches here as it's done in ProcessTouch
            break;
          case SKTouchAction.Released:
            toolStateManager.ActiveTool.OnTouchReleased(worldPoint, context);
            break;
        }
      }
    }

    private void HandleTouchPressed(SKPoint point, ToolContext context)
    {
      if (layerStateManager.CurrentLayer?.IsLocked == true) return;

      // If we're using the select tool, let it handle all selection logic
      if (toolStateManager.ActiveTool.Type == ToolType.Select)
      {
        toolStateManager.ActiveTool.OnTouchPressed(point, context);

        // Auto-select layer based on selection
        if (selectionManager.Selected.Count > 0)
        {
          var selectedElement = selectionManager.Selected[0];
          var layer = layerStateManager.Layers.FirstOrDefault(l => l.Elements.Contains(selectedElement));
          if (layer != null && layer != layerStateManager.CurrentLayer)
          {
            layerStateManager.CurrentLayer = layer;
          }
        }
        return;
      }

      // For other tools, clear selection and proceed with drawing
      if (selectionManager.Selected.Any())
      {
        selectionManager.Clear();
        messageBus.SendMessage(new CanvasInvalidateMessage());
      }

      // Pass to the active tool for drawing operations
      toolStateManager.ActiveTool.OnTouchPressed(point, context);
    }

    private ToolContext CreateToolContext()
    {
      return new ToolContext
      {
        CurrentLayer = layerStateManager.CurrentLayer!,
        StrokeColor = toolStateManager.StrokeColor,
        FillColor = toolStateManager.FillColor,
        StrokeWidth = toolStateManager.StrokeWidth,
        Opacity = toolStateManager.Opacity,
        Flow = toolStateManager.Flow,
        Spacing = toolStateManager.Spacing,
        BrushShape = toolStateManager.CurrentBrushShape,
        AllElements = layerStateManager.Layers.SelectMany(l => l.Elements),
        Layers = layerStateManager.Layers,
        SelectionManager = selectionManager,
        Scale = navigationModel.TotalMatrix.ScaleX,
        IsGlowEnabled = toolStateManager.IsGlowEnabled,
        GlowColor = toolStateManager.GlowColor,
        GlowRadius = toolStateManager.GlowRadius,
        IsRainbowEnabled = toolStateManager.IsRainbowEnabled,
        ScatterRadius = toolStateManager.ScatterRadius,
        SizeJitter = toolStateManager.SizeJitter,
        AngleJitter = toolStateManager.AngleJitter,
        HueJitter = toolStateManager.HueJitter,
        CanvasMatrix = navigationModel.UserMatrix
      };
    }
  }
}
