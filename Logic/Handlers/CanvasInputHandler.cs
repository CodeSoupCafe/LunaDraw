using LunaDraw.Logic.Managers;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Utils;

// For SKCanvasView
using Microsoft.Maui.Devices;

using ReactiveUI;

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

// For DeviceInfo and DisplayInfo

namespace LunaDraw.Logic.Services
{
  public class CanvasInputHandler : ICanvasInputHandler
  {
    private readonly IToolStateManager _toolStateManager;
    private readonly ILayerStateManager _layerStateManager;
    private readonly SelectionManager _selectionManager;
    private readonly NavigationModel _navigationModel;
    private readonly IMessageBus _messageBus;

    private readonly TouchManipulationManager _touchManipulationManager;
    private readonly Dictionary<long, SKPoint> _activeTouches = [];
    private bool _isMultiTouching = false;
    private bool _isManipulatingSelection = false;

    // Multi-touch gesture state (snapshot at gesture start)
    private SKPoint _gestureStartCentroid;
    private float _gestureStartDistance;
    private float _gestureStartAngle;
    private SKPoint _previousCentroid;
    private float _previousDistance;
    private float _previousAngle;

    // Movement deadzone threshold to reduce jitter on tiny movements
    private const float MOVEMENT_THRESHOLD = 2.0f;
    private const float SCALE_THRESHOLD = 0.001f;
    private const float ROTATION_THRESHOLD = 0.01f; // radians

    public CanvasInputHandler(
        IToolStateManager toolStateManager,
        ILayerStateManager layerStateManager,
        SelectionManager selectionManager,
        NavigationModel navigationModel,
        IMessageBus messageBus)
    {
      _toolStateManager = toolStateManager;
      _layerStateManager = layerStateManager;
      _selectionManager = selectionManager;
      _navigationModel = navigationModel;
      _messageBus = messageBus;

      _touchManipulationManager = new TouchManipulationManager();
    }

    public void ProcessTouch(SKTouchEventArgs e, SKRect canvasViewPort)
    {
      if (_layerStateManager.CurrentLayer == null) return;

      // e.Location is already relative to the SKCanvasView (pixel coordinates)
      var adjustedLocation = e.Location;

      switch (e.ActionType)
      {
        case SKTouchAction.Pressed:
          _activeTouches[e.Id] = adjustedLocation;
          break;
        case SKTouchAction.Released:
        case SKTouchAction.Cancelled:
          _activeTouches.Remove(e.Id);
          break;
      }

      // Handle Multi-Touch State Transition
      if (_activeTouches.Count >= 2)
      {
        if (!_isMultiTouching)
        {
          _isMultiTouching = true;

          // Cancel any active drawing tool
          if (_toolStateManager.ActiveTool is IDrawingTool tool)
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
        _isMultiTouching = false;
        _isManipulatingSelection = false;
      }

      // Handle Navigation (Multi-touch)
      if (_activeTouches.Count >= 2 && e.ActionType == SKTouchAction.Moved && _activeTouches.ContainsKey(e.Id))
      {
        HandleMultiTouch(adjustedLocation, e.Id);
        return;
      }

      // Handle Drawing/Tools (Single touch)
      if (_activeTouches.Count <= 1)
      {
        HandleSingleTouch(adjustedLocation, e.ActionType);
      }

      // Update stored location for Moved events if not handled by navigation
      if (e.ActionType == SKTouchAction.Moved && _activeTouches.ContainsKey(e.Id))
      {
        _activeTouches[e.Id] = adjustedLocation;
      }
    }

    private void InitializeGestureSnapshot()
    {
      // Get the two primary fingers
      var sortedKeys = _activeTouches.Keys.OrderBy(k => k).ToList();
      if (sortedKeys.Count < 2) return;

      long id1 = sortedKeys[0];
      long id2 = sortedKeys[1];

      SKPoint p1 = _activeTouches[id1];
      SKPoint p2 = _activeTouches[id2];

      // Store initial gesture state
      _gestureStartCentroid = CalculateCentroid(p1, p2);
      _gestureStartDistance = Distance(p1, p2);
      _gestureStartAngle = CalculateAngle(p1, p2);

      // Initialize "previous" values to current state
      _previousCentroid = _gestureStartCentroid;
      _previousDistance = _gestureStartDistance;
      _previousAngle = _gestureStartAngle;
    }

    private void DetermineMultiTouchTarget()
    {
      _isManipulatingSelection = false;
      var selectedElements = _selectionManager.Selected;

      if (_layerStateManager.CurrentLayer?.IsLocked == false && selectedElements.Any())
      {
        SKMatrix inverseView;
        bool canInvert = _navigationModel.TotalMatrix.TryInvert(out inverseView);

        if (canInvert)
        {
          // Check if ANY active touch is on a selected element
          foreach (var touchPoint in _activeTouches.Values)
          {
            var worldPoint = inverseView.MapPoint(touchPoint);
            if (selectedElements.Any(el => el.HitTest(worldPoint)))
            {
              _isManipulatingSelection = true;
              return;
            }
          }
        }
      }
    }

    private void HandleMultiTouch(SKPoint newLocation, long id)
    {
      // 1. Identify the two primary fingers
      var sortedKeys = _activeTouches.Keys.OrderBy(k => k).ToList();
      if (sortedKeys.Count < 2) return;

      long id1 = sortedKeys[0];
      long id2 = sortedKeys[1];

      // 2. Get CURRENT positions BEFORE updating dictionary
      // One finger is at its old position (in dictionary), the other just moved
      SKPoint currentP1 = (id == id1) ? newLocation : _activeTouches[id1];
      SKPoint currentP2 = (id == id2) ? newLocation : _activeTouches[id2];

      // 3. Calculate current gesture state
      SKPoint currentCentroid = CalculateCentroid(currentP1, currentP2);
      float currentDistance = Distance(currentP1, currentP2);
      float currentAngle = CalculateAngle(currentP1, currentP2);

      // 4. Calculate deltas from PREVIOUS frame (not from gesture start)
      SKPoint centroidDelta = currentCentroid - _previousCentroid;
      float scaleDelta = (_previousDistance > 0.001f) ? currentDistance / _previousDistance : 1.0f;
      float rotationDelta = currentAngle - _previousAngle;

      // 5. Apply thresholds to reduce jitter on tiny movements
      bool shouldTransform = false;

      if (Math.Abs(centroidDelta.X) > MOVEMENT_THRESHOLD || Math.Abs(centroidDelta.Y) > MOVEMENT_THRESHOLD)
      {
        shouldTransform = true;
      }

      if (Math.Abs(scaleDelta - 1.0f) > SCALE_THRESHOLD)
      {
        shouldTransform = true;
      }

      if (Math.Abs(rotationDelta) > ROTATION_THRESHOLD)
      {
        shouldTransform = true;
      }

      // 6. Build transformation matrix if movement exceeds threshold
      if (shouldTransform)
      {
        // Use PREVIOUS centroid as the pivot point for transformation
        SKMatrix matrix = BuildTransformationMatrix(
          _previousCentroid,
          scaleDelta,
          rotationDelta,
          centroidDelta
        );

        // Safety check for invalid matrix values
        if (!float.IsNaN(matrix.ScaleX) && !float.IsInfinity(matrix.ScaleX))
        {
          // 7. Apply to Model
          if (_isManipulatingSelection)
          {
            ApplySelectionTransform(matrix);
          }
          else
          {
            // Apply to UserMatrix (View transformation)
            _navigationModel.UserMatrix = SKMatrix.Concat(matrix, _navigationModel.UserMatrix);
          }

          MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
        }

        // 8. Update previous state for next frame
        _previousCentroid = currentCentroid;
        _previousDistance = currentDistance;
        _previousAngle = currentAngle;
      }

      // 9. Update the touch dictionary for the finger that moved
      _activeTouches[id] = newLocation;
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
      var selectedElements = _selectionManager.Selected;
      if (_layerStateManager.CurrentLayer?.IsLocked == false && selectedElements.Any())
      {
        SKMatrix inverseView;
        SKMatrix currentTotal = _navigationModel.TotalMatrix;
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
      bool canInvert = _navigationModel.TotalMatrix.TryInvert(out inverse);

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
            _toolStateManager.ActiveTool.OnTouchMoved(worldPoint, context);
            // No need to update _activeTouches here as it's done in ProcessTouch
            break;
          case SKTouchAction.Released:
            _toolStateManager.ActiveTool.OnTouchReleased(worldPoint, context);
            break;
        }
      }
    }

    private void HandleTouchPressed(SKPoint point, ToolContext context)
    {
      // If we're using the select tool, let it handle all selection logic
      if (_toolStateManager.ActiveTool.Type == ToolType.Select)
      {
        _toolStateManager.ActiveTool.OnTouchPressed(point, context);
        return;
      }

      // For other tools, clear selection and proceed with drawing
      if (_selectionManager.Selected.Any())
      {
        _selectionManager.Clear();
        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
      }

      // Pass to the active tool for drawing operations
      _toolStateManager.ActiveTool.OnTouchPressed(point, context);
    }

    private ToolContext CreateToolContext()
    {
      return new ToolContext
      {
        CurrentLayer = _layerStateManager.CurrentLayer!,
        StrokeColor = _toolStateManager.StrokeColor,
        FillColor = _toolStateManager.FillColor,
        StrokeWidth = _toolStateManager.StrokeWidth,
        Opacity = _toolStateManager.Opacity,
        Flow = _toolStateManager.Flow,
        Spacing = _toolStateManager.Spacing,
        BrushShape = _toolStateManager.CurrentBrushShape,
        AllElements = _layerStateManager.Layers.SelectMany(l => l.Elements),
        SelectionManager = _selectionManager,
        Scale = _navigationModel.TotalMatrix.ScaleX,
        IsGlowEnabled = _toolStateManager.IsGlowEnabled,
        GlowColor = _toolStateManager.GlowColor,
        GlowRadius = _toolStateManager.GlowRadius,
        IsRainbowEnabled = _toolStateManager.IsRainbowEnabled,
        ScatterRadius = _toolStateManager.ScatterRadius,
        SizeJitter = _toolStateManager.SizeJitter,
        AngleJitter = _toolStateManager.AngleJitter,
        HueJitter = _toolStateManager.HueJitter,
        CanvasMatrix = _navigationModel.UserMatrix
      };
    }
  }
}