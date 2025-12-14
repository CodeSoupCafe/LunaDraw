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
  Unicorn,
  Giraffe,
  Bear,
  Fireworks,
  Flower,
  Sun,
  Snowflake,
  Butterfly,
  Fish,
  Paw,
  Leaf,
  MusicNote,
  Smile,
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

  public static BrushShape Unicorn()
  {
    var path = new SKPath();
    path.MoveTo(-4, 0);
    path.LineTo(0, -10); // Horn tip
    path.LineTo(2, -2);
    path.LineTo(5, 0);   // Nose
    path.LineTo(4, 5);
    path.LineTo(-2, 5);
    path.LineTo(-4, 0);
    path.Close();
    // Mane
    path.MoveTo(-2, -5);
    path.QuadTo(-6, -2, -4, 2);
    return new BrushShape { Name = "Unicorn", Type = BrushShapeType.Unicorn, Path = path };
  }

  public static BrushShape Giraffe()
  {
    var path = new SKPath();
    // Side profile view
    path.MoveTo(-4, 8); // Neck base
    path.LineTo(-2, -5); // Neck up to head back
    
    // Ossicones (Horns)
    path.LineTo(-3, -11);
    path.LineTo(-1, -11);
    path.LineTo(-0.5f, -6);
    
    // Forehead to nose
    path.LineTo(1, -5);
    path.LineTo(4, 0); // Snout tip
    path.LineTo(3, 3); // Jaw
    path.LineTo(-1, 2); // Jaw back
    path.LineTo(2, 8); // Neck front
    path.Close();
    return new BrushShape { Name = "Giraffe", Type = BrushShapeType.Giraffe, Path = path };
  }

  public static BrushShape Bear()
  {
    var path = new SKPath();
    path.AddCircle(0, 0, 7); // Face
    path.AddCircle(-6, -6, 3); // Left Ear
    path.AddCircle(6, -6, 3); // Right Ear
    return new BrushShape { Name = "Bear", Type = BrushShapeType.Bear, Path = path };
  }

  public static BrushShape Fireworks()
  {
    var path = new SKPath();
    // A burst of dots/stars instead of lines
    path.AddCircle(0, 0, 2); // Center
    
    for (int i = 0; i < 8; i++)
    {
        float angle = i * 45 * (float)Math.PI / 180;
        float x = 8 * (float)Math.Sin(angle);
        float y = -8 * (float)Math.Cos(angle);
        
        // Outer dots
        path.AddCircle(x, y, 1.5f);
        
        // Middle dots
        float xm = 4 * (float)Math.Sin(angle);
        float ym = -4 * (float)Math.Cos(angle);
        path.AddCircle(xm, ym, 1.0f);
    }
    return new BrushShape { Name = "Fireworks", Type = BrushShapeType.Fireworks, Path = path };
  }

  public static BrushShape Flower()
  {
    var path = new SKPath();
    path.AddCircle(0, 0, 3); // Center
    for (int i = 0; i < 5; i++)
    {
        float angle = i * 72 * (float)Math.PI / 180;
        float cx = 6 * (float)Math.Sin(angle);
        float cy = -6 * (float)Math.Cos(angle);
        path.AddCircle(cx, cy, 3.5f);
    }
    return new BrushShape { Name = "Flower", Type = BrushShapeType.Flower, Path = path };
  }

  public static BrushShape Sun()
  {
    var path = new SKPath();
    path.AddCircle(0, 0, 6); // Core
    
    // Triangular rays
    for (int i = 0; i < 8; i++)
    {
        float angle = i * 45 * (float)Math.PI / 180;
        
        // Base of the triangle ray on the circle
        float baseAngle1 = angle - (10 * (float)Math.PI / 180);
        float baseAngle2 = angle + (10 * (float)Math.PI / 180);
        
        float x1 = 6 * (float)Math.Sin(baseAngle1);
        float y1 = -6 * (float)Math.Cos(baseAngle1);
        
        float x2 = 6 * (float)Math.Sin(baseAngle2);
        float y2 = -6 * (float)Math.Cos(baseAngle2);
        
        // Tip of the ray
        float tipX = 11 * (float)Math.Sin(angle);
        float tipY = -11 * (float)Math.Cos(angle);

        path.MoveTo(x1, y1);
        path.LineTo(tipX, tipY);
        path.LineTo(x2, y2);
        path.Close();
    }
    return new BrushShape { Name = "Sun", Type = BrushShapeType.Sun, Path = path };
  }

  public static BrushShape Snowflake()
  {
    var path = new SKPath();
    // Use rectangles for arms so they have width
    for (int i = 0; i < 3; i++) // 3 bars crossing make 6 arms
    {
        path.AddRect(new SKRect(-1.5f, -10, 1.5f, 10)); // Vertical-ish bar
        path.Transform(SKMatrix.CreateRotationDegrees(60));
    }
    // Add some details on the ends (small diamonds)
    var decorativePath = new SKPath();
    for(int i = 0; i < 6; i++)
    {
        decorativePath.AddCircle(0, -8, 2);
        decorativePath.Transform(SKMatrix.CreateRotationDegrees(60));
    }
    path.AddPath(decorativePath);
    
    return new BrushShape { Name = "Snowflake", Type = BrushShapeType.Snowflake, Path = path };
  }

  public static BrushShape Butterfly()
  {
    var path = new SKPath();
    // Body
    path.AddOval(new SKRect(-1, -6, 1, 6));
    // Wings
    path.AddOval(new SKRect(-8, -8, -1, 0)); // Top Left
    path.AddOval(new SKRect(1, -8, 8, 0));   // Top Right
    path.AddOval(new SKRect(-6, 0, -1, 6));  // Bottom Left
    path.AddOval(new SKRect(1, 0, 6, 6));    // Bottom Right
    return new BrushShape { Name = "Butterfly", Type = BrushShapeType.Butterfly, Path = path };
  }

  public static BrushShape Fish()
  {
    var path = new SKPath();
    // Body
    path.AddOval(new SKRect(-8, -5, 4, 5));
    // Tail
    path.MoveTo(4, 0);
    path.LineTo(8, -4);
    path.LineTo(8, 4);
    path.Close();
    return new BrushShape { Name = "Fish", Type = BrushShapeType.Fish, Path = path };
  }

  public static BrushShape Paw()
  {
    var path = new SKPath();
    // Main pad
    path.AddOval(new SKRect(-5, -2, 5, 6));
    // Toes
    path.AddCircle(-4, -5, 2);
    path.AddCircle(0, -6, 2);
    path.AddCircle(4, -5, 2);
    return new BrushShape { Name = "Paw", Type = BrushShapeType.Paw, Path = path };
  }

  public static BrushShape Leaf()
  {
    var path = new SKPath();
    // Wider body
    path.MoveTo(0, -10);
    path.CubicTo(8, -5, 8, 5, 0, 10);
    path.CubicTo(-8, 5, -8, -5, 0, -10);
    path.Close();
    return new BrushShape { Name = "Leaf", Type = BrushShapeType.Leaf, Path = path };
  }

  public static BrushShape MusicNote()
  {
    var path = new SKPath();
    path.AddOval(new SKRect(-4, 4, 2, 8)); // Head
    path.MoveTo(1, 6);
    path.LineTo(1, -6); // Stem
    path.LineTo(5, -4); // Flag
    path.LineTo(5, -2);
    path.LineTo(1, -4);
    path.LineTo(1, 6); // Close back to startish
    path.Close(); 
    return new BrushShape { Name = "MusicNote", Type = BrushShapeType.MusicNote, Path = path };
  }

  public static BrushShape Smile()
  {
    var path = new SKPath();
    path.FillType = SKPathFillType.EvenOdd; // Ensure holes are subtracted
    path.AddCircle(0, 0, 9); // Face
    
    // Eyes (as holes - simpler with EvenOdd, but let's reverse direction too just in case)
    path.AddCircle(-3.5f, -3, 1.5f, SKPathDirection.CounterClockwise); 
    path.AddCircle(3.5f, -3, 1.5f, SKPathDirection.CounterClockwise);
    
    // Mouth (Crescent shape)
    var mouth = new SKPath();
    mouth.MoveTo(-5, 2);
    mouth.QuadTo(0, 7, 5, 2); // Bottom curve
    mouth.QuadTo(0, 5, -5, 2); // Top curve
    mouth.Close();
    
    // Add mouth to main path (it should be treated as a hole if inside and EvenOdd or winding correct)
    path.AddPath(mouth);

    return new BrushShape { Name = "Smile", Type = BrushShapeType.Smile, Path = path };
  }
}
