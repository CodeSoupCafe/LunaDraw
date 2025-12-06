using SkiaSharp;
using SkiaSharp.Views.Maui;

// For SKCanvasView

namespace LunaDraw.Logic.Services
{
  public interface ICanvasInputHandler
  {
    void ProcessTouch(SKTouchEventArgs e, SKRect canvasViewPort);
  }
}
