using SkiaSharp;
using SkiaSharp.Views.Maui;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.Utils;
using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Tools;
using LunaDraw.Logic.Managers;
using ReactiveUI;

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
            
            _touchManipulationManager = new TouchManipulationManager
            {
                Mode = TouchManipulationMode.ScaleRotate
            };
        }

        public void ProcessTouch(SKTouchEventArgs e)
        {
            if (_layerStateManager.CurrentLayer == null) return;

            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    _activeTouches[e.Id] = e.Location;
                    break;
                case SKTouchAction.Released:
                case SKTouchAction.Cancelled:
                    _activeTouches.Remove(e.Id);
                    break;
            }

            // Handle Navigation (Multi-touch)
            if (_activeTouches.Count >= 2 && e.ActionType == SKTouchAction.Moved && _activeTouches.ContainsKey(e.Id))
            {
                HandleMultiTouch(e);
                return;
            }

            // Handle Drawing/Tools (Single touch)
            if (_activeTouches.Count <= 1)
            {
                 HandleSingleTouch(e);
            }

            // Update stored location for Moved events if not handled by navigation
            if (e.ActionType == SKTouchAction.Moved && _activeTouches.ContainsKey(e.Id))
            {
                _activeTouches[e.Id] = e.Location;
            }
        }

        private void HandleMultiTouch(SKTouchEventArgs e)
        {
            // Ensure the current finger is tracked
            if (!_activeTouches.TryGetValue(e.Id, out var prevPoint))
                return;

            // Find the pivot (any other finger)
            long pivotId = -1;
            bool hasPivot = false;
            foreach (var id in _activeTouches.Keys)
            {
                if (id != e.Id)
                {
                    pivotId = id;
                    hasPivot = true;
                    break;
                }
            }

            if (hasPivot && _activeTouches.TryGetValue(pivotId, out var pivotPoint))
            {
                var newPoint = e.Location;

                float rotation = 0;
                var touchMatrix = _touchManipulationManager.TwoFingerManipulate(prevPoint, newPoint, pivotPoint, ref rotation);

                bool handled = false;

                // Check if we should manipulate the selection instead of the view
                var selectedElements = _selectionManager.Selected;
                if (_layerStateManager.CurrentLayer?.IsLocked == false && selectedElements.Any())
                {
                    if (_navigationModel.TotalMatrix.TryInvert(out var inverseView))
                    {
                        var worldPivot = inverseView.MapPoint(pivotPoint);
                        // If the pivot finger is on a selected element, manipulate the selection
                        if (selectedElements.Any(el => el.HitTest(worldPivot)))
                        {
                            // Convert screen-space touch matrix to world-space delta
                            // delta = View^-1 * Touch * View
                            var delta = SKMatrix.Concat(inverseView, SKMatrix.Concat(touchMatrix, _navigationModel.TotalMatrix));

                            foreach (var element in selectedElements)
                            {
                                element.TransformMatrix = SKMatrix.Concat(delta, element.TransformMatrix);
                            }
                            handled = true;
                        }
                    }
                }

                if (!handled)
                {
                    // Apply transform to View
                    _navigationModel.TotalMatrix = SKMatrix.Concat(touchMatrix, _navigationModel.TotalMatrix);
                }

                // Update stored location
                _activeTouches[e.Id] = newPoint;

                MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
            }
        }

        private void HandleSingleTouch(SKTouchEventArgs e)
        {
            // Transform point to World Coordinates
            SKMatrix inverse = SKMatrix.CreateIdentity();
            if (_navigationModel.TotalMatrix.TryInvert(out inverse))
            {
                var worldPoint = inverse.MapPoint(e.Location);

                var context = new ToolContext
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

                switch (e.ActionType)
                {
                    case SKTouchAction.Pressed:
                        HandleTouchPressed(worldPoint, context);
                        break;
                    case SKTouchAction.Moved:
                        _toolStateManager.ActiveTool.OnTouchMoved(worldPoint, context);
                        if (_activeTouches.ContainsKey(e.Id)) _activeTouches[e.Id] = e.Location;
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
    }
}
