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

using SkiaSharp;

namespace LunaDraw.Logic.Models;

public enum BrushShapeType
{
  Circle,
  Square,
  Star,
  Heart,
  Sparkle,
  Cloud,
  Moon,
  Lightning,
  Diamond,
  Triangle,
  Hexagon,
  Custom
}

public class BrushShape
{
  public string Name { get; set; } = "Circle";
  public BrushShapeType Type { get; set; } = BrushShapeType.Circle;
  public SKPath Path { get; set; } = new SKPath();

  public static BrushShape Circle()
  {
    var path = new SKPath();
    path.AddCircle(0, 0, 10);
    return new BrushShape { Name = "Circle", Type = BrushShapeType.Circle, Path = path };
  }

  public static BrushShape Square()
  {
    var path = new SKPath();
    path.AddRect(new SKRect(-10, -10, 10, 10));
    return new BrushShape { Name = "Square", Type = BrushShapeType.Square, Path = path };
  }

  public static BrushShape Star()
  {
    var starPath = new SKPath();
    starPath.MoveTo(0, -10);
    starPath.LineTo(2.5f, -3.5f);
    starPath.LineTo(9.5f, -2.5f);
    starPath.LineTo(4.5f, 2.5f);
    starPath.LineTo(6f, 9.5f);
    starPath.LineTo(0, 6.5f);
    starPath.LineTo(-6f, 9.5f);
    starPath.LineTo(-4.5f, 2.5f);
    starPath.LineTo(-9.5f, -2.5f);
    starPath.LineTo(-2.5f, -3.5f);
    starPath.Close();

    return new BrushShape { Name = "Star", Type = BrushShapeType.Star, Path = starPath };
  }

  public static BrushShape Heart()
  {
    var path = new SKPath();
    // Heart shape logic
    path.MoveTo(0, 5);
    path.CubicTo(0, 5, -10, -5, -5, -10);
    path.CubicTo(-2.5f, -12.5f, 0, -7.5f, 0, -2.5f);
    path.CubicTo(0, -7.5f, 2.5f, -12.5f, 5, -10);
    path.CubicTo(10, -5, 0, 5, 0, 5);
    path.Close();

    // Center it roughly
    path.Transform(SKMatrix.CreateTranslation(0, 2.5f));

    return new BrushShape { Name = "Heart", Type = BrushShapeType.Heart, Path = path };
  }

  public static BrushShape Sparkle()
  {
    var path = new SKPath();
    // Four-pointed star / sparkle
    path.MoveTo(0, -10);
    path.QuadTo(1, -1, 10, 0);
    path.QuadTo(1, 1, 0, 10);
    path.QuadTo(-1, 1, -10, 0);
    path.QuadTo(-1, -1, 0, -10);
    path.Close();

    return new BrushShape { Name = "Sparkle", Type = BrushShapeType.Sparkle, Path = path };
  }

  public static BrushShape Cloud()
  {
    var path = new SKPath();
    path.MoveTo(-8, 0);
    path.LineTo(8, 0);
    path.ArcTo(new SKRect(4, -8, 12, 0), 0, -180, false);
    path.ArcTo(new SKRect(-4, -12, 4, -4), 0, -180, false);
    path.ArcTo(new SKRect(-12, -8, -4, 0), 0, -180, false);
    path.Close();
    path.Transform(SKMatrix.CreateTranslation(0, 4)); // Center
    return new BrushShape { Name = "Cloud", Type = BrushShapeType.Cloud, Path = path };
  }

  public static BrushShape Moon()
  {
    var path = new SKPath();
    path.AddArc(new SKRect(-10, -10, 10, 10), 30, 300);
    // Cut out the inner part
    // This is hard with basic paths without path ops (which might be heavy)
    // Let's try a simple crescent approximation with two curves
    path.Reset();
    path.MoveTo(0, -10);
    path.ArcTo(new SKRect(-10, -10, 10, 10), 270, 180, false);
    path.ArcTo(new SKRect(-5, -10, 5, 10), 90, -180, false);
    path.Close();

    return new BrushShape { Name = "Moon", Type = BrushShapeType.Moon, Path = path };
  }

  public static BrushShape Lightning()
  {
    var path = new SKPath();
    path.MoveTo(2, -10);
    path.LineTo(-5, 0);
    path.LineTo(0, 0);
    path.LineTo(-2, 10);
    path.LineTo(5, 0);
    path.LineTo(0, 0);
    path.Close();
    return new BrushShape { Name = "Lightning", Type = BrushShapeType.Lightning, Path = path };
  }

  public static BrushShape Diamond()
  {
    var path = new SKPath();
    path.MoveTo(0, -10);
    path.LineTo(7, 0);
    path.LineTo(0, 10);
    path.LineTo(-7, 0);
    path.Close();
    return new BrushShape { Name = "Diamond", Type = BrushShapeType.Diamond, Path = path };
  }

  public static BrushShape Triangle()
  {
    var path = new SKPath();
    path.MoveTo(0, -10);
    path.LineTo(9, 5);
    path.LineTo(-9, 5);
    path.Close();
    path.Transform(SKMatrix.CreateTranslation(0, 2));
    return new BrushShape { Name = "Triangle", Type = BrushShapeType.Triangle, Path = path };
  }

  public static BrushShape Hexagon()
  {
    var path = new SKPath();
    for (int i = 0; i < 6; i++)
    {
      float angle = i * 60 * (float)Math.PI / 180;
      float x = 10 * (float)Math.Sin(angle);
      float y = -10 * (float)Math.Cos(angle);
      if (i == 0) path.MoveTo(x, y);
      else path.LineTo(x, y);
    }
    path.Close();
    return new BrushShape { Name = "Hexagon", Type = BrushShapeType.Hexagon, Path = path };
  }
}
