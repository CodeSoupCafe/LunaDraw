using SkiaSharp.Views.Maui;

namespace LunaDraw.Logic.Services
{
    public interface ICanvasInputHandler
    {
        void ProcessTouch(SKTouchEventArgs e);
    }
}
