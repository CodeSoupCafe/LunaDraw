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
    private readonly Dictionary<long, SKPoint> _activeTouches = new Dictionary<long, SKPoint>();
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

    public void ProcessTouch(SKTouchEventArgs e, SKRect canvasViewPort, SKCanvasView canvasView)
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
        if (_navigationModel.TotalMatrix.TryInvert(out var inverseView))
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
      // Ensure the current finger is tracked
      if (!_activeTouches.TryGetValue(id, out var prevPoint))
        return;

      // Find the pivot (any other finger)
      long pivotId = -1;
      bool hasPivot = false;
      foreach (var keyId in _activeTouches.Keys)
      {
        if (keyId != id)
        {
          pivotId = keyId;
          hasPivot = true;
          break;
        }
      }

      if (hasPivot && _activeTouches.TryGetValue(pivotId, out var pivotPoint))
      {
        float rotation = 0;
        // Use Legacy TwoFingerManipulate
        var touchMatrix = _touchManipulationManager.TwoFingerManipulate(prevPoint, newLocation, pivotPoint, ref rotation);

        if (_isManipulatingSelection)
        {
          // Check if we should manipulate the selection instead of the view
          var selectedElements = _selectionManager.Selected;
          if (_layerStateManager.CurrentLayer?.IsLocked == false && selectedElements.Any())
          {
            if (_navigationModel.TotalMatrix.TryInvert(out var inverseView))
            {
              // Convert screen-space touch matrix to world-space delta
              // delta = View^-1 * Touch * View
              var delta = SKMatrix.Concat(inverseView, SKMatrix.Concat(touchMatrix, _navigationModel.TotalMatrix));

              foreach (var element in selectedElements)
              {
                element.TransformMatrix = SKMatrix.Concat(delta, element.TransformMatrix);
              }
            }
          }
        }
        else
        {
          // Apply transform to View using PreConcat (Touch * Total) to apply Screen Space transformation
          // This ensures the zoom/pan follows the finger in Screen Space.
          _navigationModel.TotalMatrix = SKMatrix.Concat(touchMatrix, _navigationModel.TotalMatrix);
        }

        // Update stored location
        _activeTouches[id] = newLocation;

        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
      }
    }

    private void HandleSingleTouch(SKPoint location, SKTouchAction actionType)
    {
      // Transform point to World Coordinates
      SKMatrix inverse = SKMatrix.CreateIdentity();
      
      if (_navigationModel.TotalMatrix.TryInvert(out inverse))
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
        Scale = _navigationModel.TotalMatrix.ScaleX
      };
    }
  }
}