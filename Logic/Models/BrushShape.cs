using SkiaSharp;

namespace LunaDraw.Logic.Models
{
    public enum BrushShapeType
    {
        Circle,
        Square,
        Star,
        Heart,
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
            var path = new SKPath();
            // 5-point star
            float outerRadius = 10;
            path.MoveTo(0, -outerRadius);
            for (int i = 1; i < 5; i++)
            {
                float angle = i * 4 * (float)Math.PI / 5; // 144 degrees
                path.LineTo(outerRadius * (float)Math.Sin(angle), -outerRadius * (float)Math.Cos(angle));
            }
            path.Close();
            
            // The above is a pentagram logic, let's do a proper filled star shape
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
    }
}
