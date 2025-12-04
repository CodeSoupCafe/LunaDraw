using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;

using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class FreehandTool : IDrawingTool
  {
    public string Name => "Freehand";
    public ToolType Type => ToolType.Freehand;

    private List<SKPoint>? currentPoints;
    private SKPoint lastStampPoint;
    private bool isDrawing;
    private readonly Random random = new Random();

    public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      currentPoints =
      [
        // Add initial point
        point,
      ];
      lastStampPoint = point;
      isDrawing = true;

      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchMoved(SKPoint point, ToolContext context)
    {
      if (!isDrawing || context.CurrentLayer?.IsLocked == true || currentPoints == null) return;

      float spacingPixels = context.Spacing * context.StrokeWidth;
      if (spacingPixels < 1) spacingPixels = 1;

      var vector = point - lastStampPoint;
      float distance = vector.Length;

      if (distance >= spacingPixels)
      {
        var direction = vector;
        // Normalize manually to avoid issues with zero length
        if (distance > 0)
        {
          float invLength = 1.0f / distance;
          direction = new SKPoint(direction.X * invLength, direction.Y * invLength);
        }

        int steps = (int)(distance / spacingPixels);
        for (int i = 0; i < steps; i++)
        {
          var idealPoint = lastStampPoint + new SKPoint(direction.X * spacingPixels, direction.Y * spacingPixels);
          
          var finalPoint = idealPoint;
          if (context.ScatterRadius > 0)
          {
              // Random scatter in a circle
              double angle = random.NextDouble() * Math.PI * 2;
              double r = Math.Sqrt(random.NextDouble()) * context.ScatterRadius; // Sqrt for uniform distribution
              finalPoint += new SKPoint((float)(r * Math.Cos(angle)), (float)(r * Math.Sin(angle)));
          }

          currentPoints.Add(finalPoint);
          lastStampPoint = idealPoint;
        }

        MessageBus.Current.SendMessage(new CanvasInvalidateMessage());

      }
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (!isDrawing || context.CurrentLayer == null || context.CurrentLayer.IsLocked || currentPoints == null) return;

      if (currentPoints.Count > 0)
      {
        var element = new DrawableStamps
        {
          Points = new List<SKPoint>(currentPoints),
          Shape = context.BrushShape,
          Size = context.StrokeWidth,
          Flow = context.Flow,
          Opacity = context.Opacity,
          StrokeColor = context.StrokeColor,
          IsGlowEnabled = context.IsGlowEnabled,
          GlowColor = context.GlowColor,
          GlowRadius = context.GlowRadius,
          IsRainbowEnabled = context.IsRainbowEnabled,
          SizeJitter = context.SizeJitter,
          AngleJitter = context.AngleJitter,
          HueJitter = context.HueJitter
        };

        context.CurrentLayer.Elements.Add(element);
        MessageBus.Current.SendMessage(new DrawingStateChangedMessage());
      }

      currentPoints = null;
      isDrawing = false;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchCancelled(ToolContext context)
    {
      currentPoints = null;
      isDrawing = false;
      MessageBus.Current.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, MainViewModel viewModel)
    {
      if (currentPoints == null || currentPoints.Count == 0) return;

      // Get current shape from viewModel
      var shape = viewModel.CurrentBrushShape;
      if (shape?.Path == null) return;

      float size = viewModel.StrokeWidth;
      float baseScale = size / 20f;
      byte flow = viewModel.Flow;
      byte opacity = viewModel.Opacity;

      using var scaledPath = new SKPath(shape.Path);
      var scaleMatrix = SKMatrix.CreateScale(baseScale, baseScale);
      scaledPath.Transform(scaleMatrix);

      using var paint = new SKPaint
      {
          Style = SKPaintStyle.Fill,
          IsAntialias = true
      };

      int index = 0;
      foreach (var point in currentPoints)
      {
        // Local random for preview jitter
        var random = new Random(index * 1337); 

        // Calculate color
        SKColor color = viewModel.StrokeColor;
        if (viewModel.IsRainbowEnabled)
        {
            float hue = (index * 10) % 360;
            color = SKColor.FromHsl(hue, 100, 50);
        }
        else if (viewModel.HueJitter > 0)
        {
            color.ToHsl(out float h, out float s, out float l);
            float jitter = ((float)random.NextDouble() - 0.5f) * 2.0f * viewModel.HueJitter * 360f;
            h = (h + jitter) % 360f;
            if (h < 0) h += 360f;
            color = SKColor.FromHsl(h, s, l);
        }
        
        paint.Color = color.WithAlpha((byte)(flow * (opacity / 255f)));

        canvas.Save();
        canvas.Translate(point.X, point.Y);

        // Rotation Jitter
        if (viewModel.AngleJitter > 0)
        {
            float rotation = ((float)random.NextDouble() - 0.5f) * 2.0f * viewModel.AngleJitter;
            canvas.RotateDegrees(rotation);
        }

        // Size Jitter
        if (viewModel.SizeJitter > 0)
        {
            float scaleFactor = 1.0f + ((float)random.NextDouble() - 0.5f) * 2.0f * viewModel.SizeJitter;
            if (scaleFactor < 0.1f) scaleFactor = 0.1f;
            canvas.Scale(scaleFactor);
        }

        canvas.DrawPath(scaledPath, paint);
        canvas.Restore();

        index++;
      }
    }
  }
}
