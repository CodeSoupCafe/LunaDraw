using SkiaSharp;

namespace LunaDraw.Logic.Models
{       
    public class DrawablePath
    {       
        public SKPath Path { get; set; }
        public SKColor Color { get; set; }
        public float StrokeWidth { get; set; }
    }
}       
    