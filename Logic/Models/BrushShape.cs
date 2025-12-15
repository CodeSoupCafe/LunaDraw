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
  Elephant,
  Tiger,
  Monkey,
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
    // Neck base
    path.MoveTo(-2, 10);
    // Chest/Neck front
    path.QuadTo(0, 5, 2, 0); 
    // Snout
    path.LineTo(6, 2);
    path.LineTo(5, -2);
    // Forehead
    path.LineTo(1, -5);
    // Horn
    path.LineTo(2, -12);
    path.LineTo(0, -6);
    // Ears/Top of head
    path.LineTo(-1, -7);
    path.LineTo(-2, -5);
    // Mane/Neck back
    path.QuadTo(-5, -2, -6, 10);
    path.Close();
    
    return new BrushShape { Name = "Unicorn", Type = BrushShapeType.Unicorn, Path = path };
  }

  public static BrushShape Giraffe()
  {
    var path = new SKPath();
    // Neck base
    path.MoveTo(-1, 10);
    // Neck front
    path.LineTo(0, -5);
    // Jaw
    path.LineTo(4, -4);
    // Snout
    path.LineTo(4, -7);
    // Forehead
    path.LineTo(1, -9);
    // Ossicones (Horns)
    path.LineTo(1.5f, -12);
    path.LineTo(0.5f, -12);
    path.LineTo(0.5f, -9);
    // Head back
    path.LineTo(-1, -8);
    // Neck back
    path.LineTo(-3, 10);
    path.Close();
    return new BrushShape { Name = "Giraffe", Type = BrushShapeType.Giraffe, Path = path };
  }

  public static BrushShape Bear()
  {
    var path = new SKPath();
    path.FillType = SKPathFillType.EvenOdd; // Use EvenOdd for holes
    path.AddCircle(0, 0, 7); // Face
    path.AddCircle(-6, -6, 3); // Left Ear
    path.AddCircle(6, -6, 3); // Right Ear

    // Eyes (Holes)
    path.AddCircle(-2.5f, -2, 1f, SKPathDirection.CounterClockwise);
    path.AddCircle(2.5f, -2, 1f, SKPathDirection.CounterClockwise);

    // Nose/Mouth area (Hole)
    var snout = new SKPath();
    snout.AddOval(new SKRect(-3, 1, 3, 5));
    path.AddPath(snout);

    return new BrushShape { Name = "Bear", Type = BrushShapeType.Bear, Path = path };
  }

  public static BrushShape Elephant()
  {
    var path = new SKPath();
    path.FillType = SKPathFillType.EvenOdd;
    // Full body profile facing Right
    path.MoveTo(-9, 5); // Back leg bottom
    path.LineTo(-9, -2); // Rump
    path.QuadTo(-8, -6, -4, -6); // Back
    path.LineTo(0, -5); // Shoulder area
    path.QuadTo(2, -7, 6, -5); // Head top
    path.LineTo(7, -3); // Forehead
    path.QuadTo(9, -1, 9, 3); // Trunk outer top
    path.LineTo(8, 4); // Trunk tip
    path.LineTo(7, 3); // Trunk inner tip
    path.QuadTo(7, 0, 6, 1); // Trunk inner curve
    path.LineTo(6, 5); // Front leg
    path.LineTo(4, 5); // Front leg width
    path.LineTo(4, 2); // Under belly start
    path.QuadTo(0, 3, -5, 2); // Belly
    path.LineTo(-5, 5); // Back leg inner
    path.Close();

    // Big Ear
    var ear = new SKPath();
    ear.MoveTo(1, -4);
    ear.QuadTo(4, -5, 5, -1);
    ear.QuadTo(4, 2, 2, 1);
    ear.Close();
    path.AddPath(ear);

    // Eye (Hole)
    path.AddCircle(5, -3.5f, 0.6f, SKPathDirection.CounterClockwise);

    return new BrushShape { Name = "Elephant", Type = BrushShapeType.Elephant, Path = path };
  }

  public static BrushShape Tiger()
  {
    var path = new SKPath();
    path.FillType = SKPathFillType.EvenOdd;
    path.AddCircle(0, 0, 7); // Face
    // Ears
    path.MoveTo(-5, -5);
    path.LineTo(-7, -9);
    path.LineTo(-3, -7);
    path.Close();
    
    path.MoveTo(5, -5);
    path.LineTo(7, -9);
    path.LineTo(3, -7);
    path.Close();
    
    // Eyes (Holes)
    path.AddCircle(-2.5f, -2, 1f, SKPathDirection.CounterClockwise);
    path.AddCircle(2.5f, -2, 1f, SKPathDirection.CounterClockwise);

    // Nose (Hole)
    var nose = new SKPath();
    nose.MoveTo(-1, 2);
    nose.LineTo(1, 2);
    nose.LineTo(0, 4);
    nose.Close();
    path.AddPath(nose);

    // Stripes (Holes on cheeks/forehead)
    var stripes = new SKPath();
    // Left cheek
    stripes.MoveTo(-7, 0);
    stripes.LineTo(-4, 1);
    stripes.LineTo(-7, 2);
    stripes.Close();
    // Right cheek
    stripes.MoveTo(7, 0);
    stripes.LineTo(4, 1);
    stripes.LineTo(7, 2);
    stripes.Close();
    // Forehead
    stripes.MoveTo(0, -7);
    stripes.LineTo(-1, -5);
    stripes.LineTo(1, -5);
    stripes.Close();
    
    path.AddPath(stripes);

    return new BrushShape { Name = "Tiger", Type = BrushShapeType.Tiger, Path = path };
  }

  public static BrushShape Monkey()
  {
    var path = new SKPath();
    path.FillType = SKPathFillType.EvenOdd;
    path.AddCircle(0, 0, 6); // Face
    path.AddCircle(-7, 0, 2.5f); // Left Ear
    path.AddCircle(7, 0, 2.5f); // Right Ear
    
    // Hair tuft
    path.MoveTo(0, -6);
    path.LineTo(-1, -8);
    path.LineTo(1, -8);
    path.Close();

    // Eyes (Holes)
    path.AddCircle(-2, -1, 1f, SKPathDirection.CounterClockwise);
    path.AddCircle(2, -1, 1f, SKPathDirection.CounterClockwise);

    // Mouth (Hole)
    var mouth = new SKPath();
    mouth.MoveTo(-2, 3);
    mouth.QuadTo(0, 5, 2, 3);
    mouth.Close(); // Thin smile
    path.AddPath(mouth);

    return new BrushShape { Name = "Monkey", Type = BrushShapeType.Monkey, Path = path };
  }

  public static BrushShape Fireworks()
  {
    var path = new SKPath();
    // Balanced burst with gravity arc
    path.MoveTo(0, -2); // Center slightly up

    for (int i = 0; i < 8; i++)
    {
        float angleDeg = i * 45;
        // Skip bottom ones to look like they are falling from top
        if (angleDeg > 135 && angleDeg < 225) continue; 

        float angle = angleDeg * (float)Math.PI / 180;
        
        // Start point near center
        float sx = 2 * (float)Math.Sin(angle);
        float sy = -2 * (float)Math.Cos(angle);

        // Control point (outward)
        float cx = 8 * (float)Math.Sin(angle);
        float cy = -8 * (float)Math.Cos(angle);

        // End point (drooping down)
        // Add gravity component to Y
        float ex = 10 * (float)Math.Sin(angle);
        float ey = -10 * (float)Math.Cos(angle) + 4; 

        // Draw trail as a tapered shape
        path.MoveTo(sx, sy);
        path.QuadTo(cx, cy, ex, ey); // Curve out and down
        
        // Taper back
        path.LineTo(ex + 0.5f, ey); // Small width at tip
        path.QuadTo(cx + 0.5f, cy, sx + 1f, sy); // Return curve
        path.Close();
    }

    // Add a few loose sparks
    path.AddCircle(0, -5, 1.2f);
    path.AddCircle(-4, -2, 1f);
    path.AddCircle(4, -2, 1f);
    
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
    // Body (Flipped to face right)
    path.AddOval(new SKRect(-4, -5, 8, 5));
    // Tail (Flipped to left side)
    path.MoveTo(-4, 0);
    path.LineTo(-8, -4);
    path.LineTo(-8, 4);
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
    
    // Stem (Rectangle for thickness)
    path.MoveTo(1, 6);
    path.LineTo(2, 6);
    path.LineTo(2, -6);
    path.LineTo(1, -6);
    path.Close();

    // Flag (Polygon for thickness)
    path.MoveTo(2, -6);
    path.LineTo(6, -2); // Tip outer
    path.LineTo(6, -0.5f); // Tip inner
    path.QuadTo(4, -3, 2, -2); // Inner curve
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
