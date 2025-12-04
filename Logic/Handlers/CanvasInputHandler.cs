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
       
       // 2. Get "Previous" points (Current state of _activeTouches)
       SKPoint prevP1 = _activeTouches[id1];
       SKPoint prevP2 = _activeTouches[id2];
       
       // 3. Determine "New" points
       // One of these is the finger that moved ('id').
       SKPoint newP1 = (id == id1) ? newLocation : prevP1;
       SKPoint newP2 = (id == id2) ? newLocation : prevP2;
       
       // 4. Calculate Canonical Transformations
       // Pivot is the midpoint of the PREVIOUS state
       SKPoint prevCenter = new SKPoint((prevP1.X + prevP2.X) / 2.0f, (prevP1.Y + prevP2.Y) / 2.0f);
       SKPoint newCenter = new SKPoint((newP1.X + newP2.X) / 2.0f, (newP1.Y + newP2.Y) / 2.0f);
       
       float prevDist = Distance(prevP1, prevP2);
       float newDist = Distance(newP1, newP2);
       float scale = (prevDist > 0.001f) ? newDist / prevDist : 1.0f;
       
       float prevAngle = (float)Math.Atan2(prevP2.Y - prevP1.Y, prevP2.X - prevP1.X);
       float newAngle = (float)Math.Atan2(newP2.Y - newP1.Y, newP2.X - newP1.X);
       float rotationDelta = newAngle - prevAngle; // Radians
       
       // 5. Construct Matrix: 
       // Translate(-Pivot) -> Scale & Rotate -> Translate(Pivot) -> Translate(Pan)
       
       SKMatrix matrix = SKMatrix.CreateIdentity();
       
       // Move Pivot to Origin
       matrix = matrix.PostConcat(SKMatrix.CreateTranslation(-prevCenter.X, -prevCenter.Y));
       
       // Scale (around origin)
       matrix = matrix.PostConcat(SKMatrix.CreateScale(scale, scale));
       
       // Rotate (around origin)
       matrix = matrix.PostConcat(SKMatrix.CreateRotation(rotationDelta));
       
       // Move Pivot back
       matrix = matrix.PostConcat(SKMatrix.CreateTranslation(prevCenter.X, prevCenter.Y));
       
       // Apply Pan (NewCenter - PrevCenter)
       SKPoint pan = newCenter - prevCenter;
       matrix = matrix.PostConcat(SKMatrix.CreateTranslation(pan.X, pan.Y));

       // Safety check for invalid matrix values
       if (float.IsNaN(matrix.ScaleX) || float.IsInfinity(matrix.ScaleX)) return;

       // 6. Apply to Model
       if (_isManipulatingSelection)
       {
            ApplySelectionTransform(matrix);
       }
       else
       {
           // Apply to UserMatrix
           // Since UserMatrix is our View Matrix, and 'matrix' is a Screen-Space transformation (Delta),
           // we Pre-Concat the Delta to the View Matrix.
           // NewView = Delta * OldView
           _navigationModel.UserMatrix = SKMatrix.Concat(matrix, _navigationModel.UserMatrix);
       }
       
       // 7. Update the state for the next event
       _activeTouches[id] = newLocation;
       
       MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
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