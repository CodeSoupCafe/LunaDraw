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

using LunaDraw.Logic.Messages;
using LunaDraw.Logic.Models;
using LunaDraw.Logic.ViewModels;
using ReactiveUI;

using SkiaSharp;

namespace LunaDraw.Logic.Tools
{
  public class FreehandTool(IMessageBus messageBus) : IDrawingTool
  {
    public string Name => "Stamps";
    public ToolType Type => ToolType.Freehand;

    private List<(SKPoint Point, float Rotation)>? currentPoints;
    private SKPoint lastStampPoint;
    private bool isDrawing;
    private readonly Random random = new Random();
    private readonly IMessageBus messageBus = messageBus;

        public void OnTouchPressed(SKPoint point, ToolContext context)
    {
      if (context.CurrentLayer?.IsLocked == true) return;

      currentPoints =
      [
        // Add initial point with default 0 rotation
        (point, 0f),
      ];
      lastStampPoint = point;
      isDrawing = true;

      messageBus.SendMessage(new CanvasInvalidateMessage());
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

        // Calculate angle for this segment
        float angle = (float)(Math.Atan2(vector.Y, vector.X) * 180.0 / Math.PI);

        int steps = (int)(distance / spacingPixels);
        for (int i = 0; i < steps; i++)
        {
          var idealPoint = lastStampPoint + new SKPoint(direction.X * spacingPixels, direction.Y * spacingPixels);
          
          var finalPoint = idealPoint;
          if (context.ScatterRadius > 0)
          {
              // Random scatter in a circle
              double rndAngle = random.NextDouble() * Math.PI * 2;
              double r = Math.Sqrt(random.NextDouble()) * context.ScatterRadius; // Sqrt for uniform distribution
              finalPoint += new SKPoint((float)(r * Math.Cos(rndAngle)), (float)(r * Math.Sin(rndAngle)));
          }

          currentPoints.Add((finalPoint, angle));
          lastStampPoint = idealPoint;
        }

        messageBus.SendMessage(new CanvasInvalidateMessage());

      }
    }

    public void OnTouchReleased(SKPoint point, ToolContext context)
    {
      if (!isDrawing || context.CurrentLayer == null || context.CurrentLayer.IsLocked || currentPoints == null) return;

      if (currentPoints.Count > 0)
      {
        var points = currentPoints.Select(p => p.Point).ToList();
        var rotations = currentPoints.Select(p => p.Rotation).ToList();

        var element = new DrawableStamps
        {
          Points = points,
          Rotations = rotations,
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
        messageBus.SendMessage(new DrawingStateChangedMessage());
      }

      currentPoints = null;
      isDrawing = false;
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void OnTouchCancelled(ToolContext context)
    {
      currentPoints = null;
      isDrawing = false;
      messageBus.SendMessage(new CanvasInvalidateMessage());
    }

    public void DrawPreview(SKCanvas canvas, ToolContext context)
    {
      if (currentPoints == null || currentPoints.Count == 0) return;

      // Get current shape from context
      var shape = context.BrushShape;
      if (shape?.Path == null) return;

      float size = context.StrokeWidth;
      float baseScale = size / 20f;
      byte flow = context.Flow;
      byte opacity = context.Opacity;

      using var scaledPath = new SKPath(shape.Path);
      var scaleMatrix = SKMatrix.CreateScale(baseScale, baseScale);
      scaledPath.Transform(scaleMatrix);

      using var paint = new SKPaint
      {
          Style = SKPaintStyle.Fill,
          IsAntialias = true
      };

      int index = 0;
      
      foreach (var item in currentPoints)
      {
        var point = item.Point;
        var baseRotation = item.Rotation;

        // Local random for preview jitter (Must match DrawableStamps logic)
        int seed;
        unchecked 
        {
            seed = 17;
            seed = seed * 23 + point.X.GetHashCode();
            seed = seed * 23 + point.Y.GetHashCode();
        }
        var random = new Random(seed); 

        // 1. Size Jitter (Consume randoms first)
        float scaleFactor = 1.0f;
        if (context.SizeJitter > 0)
        {
            float unusedJitter = (float)random.NextDouble() * context.SizeJitter; 
            scaleFactor = 1.0f + ((float)random.NextDouble() - 0.5f) * 2.0f * context.SizeJitter;
            if (scaleFactor < 0.1f) scaleFactor = 0.1f;
        }

        // 2. Angle Jitter
        float rotationDelta = 0f;
        if (context.AngleJitter > 0)
        {
            rotationDelta = ((float)random.NextDouble() - 0.5f) * 2.0f * context.AngleJitter;
        }

        // 3. Color (Hue) Jitter
        SKColor color = context.StrokeColor;
        if (context.IsRainbowEnabled)
        {
            float hue = index * 10 % 360;
            color = SKColor.FromHsl(hue, 100, 50);
        }
        else if (context.HueJitter > 0)
        {
            color.ToHsl(out float h, out float s, out float l);
            float jitter = ((float)random.NextDouble() - 0.5f) * 2.0f * context.HueJitter * 360f;
            h = (h + jitter) % 360f;
            if (h < 0) h += 360f;
            color = SKColor.FromHsl(h, s, l);
        }
        
        paint.Color = color.WithAlpha((byte)(flow * (opacity / 255f)));

        canvas.Save();
        canvas.Translate(point.X, point.Y);

        // Apply Stroke Rotation
        if (Math.Abs(baseRotation) > 0.001f)
        {
            canvas.RotateDegrees(baseRotation);
        }

        // Apply Jitter Rotation
        if (context.AngleJitter > 0)
        {
            canvas.RotateDegrees(rotationDelta);
        }

        // Apply Jitter Scale
        if (scaleFactor != 1.0f)
        {
            canvas.Scale(scaleFactor);
        }

        canvas.DrawPath(scaledPath, paint);
        canvas.Restore();

        index++;
      }
    }
  }
}