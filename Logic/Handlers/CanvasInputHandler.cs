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
      if (layerStateManager.CurrentLayer == null) return;

      var location = e.Location;

      // Right click = select
      if (e.MouseButton == SKMouseButton.Right)
      {
        if (e.ActionType == SKTouchAction.Pressed)
        {
          var selectTool = toolStateManager.AvailableTools.FirstOrDefault(t => t.Type == ToolType.Select);
          if (selectTool != null) toolStateManager.ActiveTool = selectTool;

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
          if (toolStateManager.ActiveTool is IDrawingTool tool)
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
          if (layerStateManager.CurrentLayer?.IsLocked == false && selectionManager.Selected.Any())
          {
            if (navigationModel.ViewMatrix.TryInvert(out var inv))
            {
              foreach (var touch in activeTouches.Values)
              {
                var worldPt = inv.MapPoint(touch);
                if (selectionManager.Selected.Any(el => el.HitTest(worldPt)))
                {
                  manipulatingSelection = true;
                  startElementMatrices = selectionManager.Selected.ToDictionary(el => el, el => el.TransformMatrix);
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
              toolStateManager.ActiveTool.OnTouchMoved(worldPoint, context);
              break;
            case SKTouchAction.Released:
              toolStateManager.ActiveTool.OnTouchReleased(worldPoint, context);
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

          foreach (var element in selectionManager.Selected)
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

      foreach (var layer in layerStateManager.Layers.Reverse())
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
        if (!selectionManager.Contains(hit))
        {
          selectionManager.Clear();
          selectionManager.Add(hit);
        }
        if (hitLayer != null) layerStateManager.CurrentLayer = hitLayer;
      }
      else
      {
        selectionManager.Clear();
      }

      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    private void HandleTouchPressed(SKPoint worldPoint, ToolContext context)
    {
      if (layerStateManager.CurrentLayer?.IsLocked == true) return;

      if (toolStateManager.ActiveTool.Type == ToolType.Select)
      {
        toolStateManager.ActiveTool.OnTouchPressed(worldPoint, context);

        if (selectionManager.Selected.Count > 0)
        {
          var layer = layerStateManager.Layers.FirstOrDefault(l => l.Elements.Contains(selectionManager.Selected[0]));
          if (layer != null && layer != layerStateManager.CurrentLayer)
          {
            layerStateManager.CurrentLayer = layer;
          }
        }
        return;
      }

      if (selectionManager.Selected.Any())
      {
        selectionManager.Clear();
        messageBus.SendMessage(new CanvasInvalidateMessage());
      }

      toolStateManager.ActiveTool.OnTouchPressed(worldPoint, context);
    }

    private float Distance(SKPoint p1, SKPoint p2) =>
      (float)Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));

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
        Scale = navigationModel.ViewMatrix.ScaleX,
        IsGlowEnabled = toolStateManager.IsGlowEnabled,
        GlowColor = toolStateManager.GlowColor,
        GlowRadius = toolStateManager.GlowRadius,
        IsRainbowEnabled = toolStateManager.IsRainbowEnabled,
        ScatterRadius = toolStateManager.ScatterRadius,
        SizeJitter = toolStateManager.SizeJitter,
        AngleJitter = toolStateManager.AngleJitter,
        HueJitter = toolStateManager.HueJitter,
        CanvasMatrix = navigationModel.ViewMatrix
      };
    }
  }
}